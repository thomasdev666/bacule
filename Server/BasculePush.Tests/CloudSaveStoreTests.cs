using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bascule.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace BasculePush.Tests
{
    /// <summary>
    /// Le module parle au vrai client Cloud Save de Unity : bonne méthode pour chaque type de données (publiques, par
    /// défaut, protégées), jeton du service, bon projet. Une erreur ici passerait tous les autres tests.
    /// </summary>
    public class CloudSaveStoreTests
    {
        readonly FakeContext _context = new FakeContext();

        (CloudSaveStore store, List<(string method, object[] args)> calls) Make(List<Item> items = null, Exception error = null)
        {
            var calls = new List<(string, object[])>();
            var api = GameApiProxy.Create(CloudSaveProxy.Create(calls, items, error));
            return (new CloudSaveStore(_context, api), calls);
        }

        static void AssertRead(List<(string method, object[] args)> calls, string method, string player, params string[] keys)
        {
            var (name, args) = calls.Single();
            Assert.That(name, Is.EqualTo(method));
            Assert.That(args[1], Is.EqualTo("jeton-service"), "jeton du service, pas celui du joueur");
            Assert.That(args[2], Is.EqualTo("projet-test"));
            Assert.That(args[3], Is.EqualTo(player));
            Assert.That((List<string>)args[4], Is.EqualTo(keys));
        }

        [Test]
        public async Task PublicDataOfTheInviter()
        {
            var (store, calls) = Make(new List<Item> { Helpers.Item(DuelPush.DuelToKey, "Target2"), Helpers.Item(DuelPush.DuelAtKey, 1800000000L) });
            var data = await store.GetPublicAsync("Inviter1", DuelPush.DuelToKey, DuelPush.DuelAtKey);
            AssertRead(calls, "GetPublicItemsAsync", "Inviter1", DuelPush.DuelToKey, DuelPush.DuelAtKey);
            Assert.That(data[DuelPush.DuelToKey], Is.EqualTo("Target2"));
            Assert.That(data[DuelPush.DuelAtKey], Is.EqualTo("1800000000"), "un nombre est relu en texte");
        }

        [Test]
        public async Task DefaultDataOfTheTarget()
        {
            var (store, calls) = Make(new List<Item> { Helpers.Item(DuelPush.TokenKey, "abc") });
            var data = await store.GetDefaultAsync("Target2", DuelPush.TokenKey);
            AssertRead(calls, "GetItemsAsync", "Target2", DuelPush.TokenKey);
            Assert.That(data[DuelPush.TokenKey], Is.EqualTo("abc"));
        }

        [Test]
        public async Task ProtectedData()
        {
            var (store, calls) = Make(new List<Item>());
            var data = await store.GetProtectedAsync("Inviter1", DuelPush.SentLogKey);
            AssertRead(calls, "GetProtectedItemsAsync", "Inviter1", DuelPush.SentLogKey);
            Assert.That(data, Is.Empty);
        }

        [Test]
        public async Task WritesUseTheRightAccessClass()
        {
            var (store, calls) = Make();
            await store.SetProtectedAsync("Inviter1", new Dictionary<string, string> { [DuelPush.SentLogKey] = "1:Target2", [DuelPush.StatusKey] = "sent" });
            await store.SetDefaultAsync("Target2", new Dictionary<string, string> { [DuelPush.TokenKey] = null });
            Assert.That(calls.Select(c => c.method), Is.EqualTo(new[] { "SetProtectedItemBatchAsync", "SetItemBatchAsync" }));
            var batch = (SetItemBatchBody)calls[0].args[4];
            Assert.That(calls[0].args[1], Is.EqualTo("jeton-service"));
            Assert.That(calls[0].args[3], Is.EqualTo("Inviter1"));
            Assert.That(batch.Data.Select(d => d.Key), Is.EqualTo(new[] { DuelPush.SentLogKey, DuelPush.StatusKey }));
            Assert.That(batch.Data[0].Value, Is.EqualTo("1:Target2"));
            var clear = (SetItemBatchBody)calls[1].args[4];
            Assert.That(calls[1].args[3], Is.EqualTo("Target2"));
            Assert.That(clear.Data.Single().Value, Is.EqualTo(""), "jamais null : Cloud Save exige une valeur ; le texte vide efface le jeton");
        }

        [Test]
        public async Task PlayerWithoutDataReadsAsEmpty()
        {
            var (store, _) = Make(error: Helpers.NotFound());
            Assert.That(await store.GetDefaultAsync("Target2", DuelPush.TokenKey), Is.Empty);
            Assert.That(await store.GetPublicAsync("Target2", DuelPush.DuelToKey), Is.Empty);
            Assert.That(await store.GetProtectedAsync("Target2", DuelPush.ReceivedLogKey), Is.Empty);
        }

        [Test]
        public void OtherErrorsAreNotHidden()
        {
            var (store, _) = Make(error: new InvalidOperationException("panne"));
            Assert.ThrowsAsync<InvalidOperationException>(() => store.GetDefaultAsync("Target2", DuelPush.TokenKey));
        }

        [Test]
        public async Task SecretsAreReadFromTheSecretManager()
        {
            var secrets = new FakeSecrets();
            var api = GameApiProxy.Create(null, secrets);
            var source = new SecretManagerCredentials(_context, api);
            Assert.That(await source.GetAsync(), Is.Null, "rien de rangé : pas de clé");
            secrets.Values["APNS_KEY"] = "clé";
            secrets.Values["APNS_KEY_ID"] = "KEY1234567";
            Assert.That(await source.GetAsync(), Is.Null, "il en manque une");
            secrets.Values["APNS_TEAM_ID"] = "TEAM765432";
            var c = await source.GetAsync();
            Assert.That(c.PrivateKey, Is.EqualTo("clé"));
            Assert.That(c.KeyId, Is.EqualTo("KEY1234567"));
            Assert.That(c.TeamId, Is.EqualTo("TEAM765432"));
        }
    }

    /// <summary>Ce que Cloud Code voit du module : la fonction, son paramètre, et les services qu'elle reçoit.</summary>
    public class ModuleShapeTests
    {
        [Test]
        public void FunctionIsExposedUnderTheNameTheGameCalls()
        {
            var method = typeof(DuelPushModule).GetMethods().Single(m => m.GetCustomAttribute<CloudCodeFunctionAttribute>() != null);
            Assert.That(method.GetCustomAttribute<CloudCodeFunctionAttribute>().Name, Is.EqualTo(DuelPush.Function));
            Assert.That(method.ReturnType, Is.EqualTo(typeof(Task<string>)));
            // Le seul paramètre qui vient du jeu ({"params":{"targetId":…}}) ; les autres sont donnés par Cloud Code.
            var fromGame = method.GetParameters().Where(p => p.ParameterType == typeof(string)).Select(p => p.Name).ToList();
            Assert.That(fromGame, Is.EqualTo(new[] { DuelPush.TargetParam }));
            Assert.That(method.GetParameters().Select(p => p.ParameterType),
                Is.EquivalentTo(new[] { typeof(IExecutionContext), typeof(IGameApiClient), typeof(IApnsSender), typeof(string) }));
        }

        sealed class Config : ICloudCodeConfig
        {
            public IServiceCollection Dependencies { get; } = new ServiceCollection();
            public string GetString(string key, string defaultValue) => defaultValue;
            public void SetString(string key, string value) { }
        }

        [Test]
        public void SetupGivesEveryServiceTheFunctionNeeds()
        {
            var config = new Config();
            new ModuleConfig().Setup(config);
            using (var provider = config.Dependencies.BuildServiceProvider())
            {
                Assert.That(provider.GetService<IGameApiClient>(), Is.Not.Null);
                Assert.That(provider.GetService<IApnsSender>(), Is.InstanceOf<ApnsSender>());
            }
        }

        [Test]
        public void SharedKeysMatchTheGame()
        {
            // Mêmes noms que ceux écrits par le jeu (UgsLeaderboardService) : un écart et plus rien ne part.
            Assert.That(DuelPush.DuelToKey, Is.EqualTo("duel_to"));
            Assert.That(DuelPush.DuelCodeKey, Is.EqualTo("duel_code"));
            Assert.That(DuelPush.DuelAtKey, Is.EqualTo("duel_at"));
            Assert.That(DuelPush.DuelNameKey, Is.EqualTo("duel_name"));
            Assert.That(DuelPush.Module, Is.EqualTo("BasculePush"));
            Assert.That(typeof(DuelPushModule).Assembly.GetName().Name, Is.EqualTo(DuelPush.Module), "Tools/deploy_push_module.py déploie le module sous le nom de son assembly");
        }
    }
}
