mergeInto(LibraryManager.library, {
  // Panneau de partage de la version web, dans le style du jeu : voile violet et rayons qui tournent (comme
  // l'écran de fin), titre blanc cerné, carte de score qui apparaît en rebondissant, boutons « bonbon » faits
  // avec les vraies images du jeu (TemplateData/ui, teintées aux couleurs de l'interface), bouton fermer rond.
  // Les boutons sont de vrais boutons de la page : le navigateur n'autorise le partage et la copie qu'en réponse
  // directe à un toucher.
  BasculeShowShare: function (imgPtr, textPtr, labelsPtr) {
    var b64 = imgPtr ? UTF8ToString(imgPtr) : "";
    var text = UTF8ToString(textPtr);
    var L = {};
    try { L = JSON.parse(UTF8ToString(labelsPtr)); } catch (e) {}
    var UI = "TemplateData/ui/";

    if (!document.getElementById("bascule-share-style")) {
      var css = document.createElement("style");
      css.id = "bascule-share-style";
      css.textContent = [
        ".bsh{position:fixed;left:0;top:0;right:0;bottom:0;z-index:50;overflow:hidden;font-family:Fredoka,'Arial Rounded MT Bold',system-ui,sans-serif;color:#fff;-webkit-user-select:none;user-select:none;-webkit-tap-highlight-color:transparent;",
        "background:radial-gradient(ellipse at 50% 32%,rgba(92,70,170,.78) 0%,rgba(38,26,78,.92) 55%,rgba(18,13,38,.96) 100%);animation:bsh-fade .25s ease-out}",
        ".bsh.out{animation:bsh-fadeout .2s ease-in forwards}",
        ".bsh-rays{position:absolute;left:50%;top:36%;width:170vmax;height:170vmax;margin:-85vmax 0 0 -85vmax;background:url(" + UI + "rayons.png) center/contain no-repeat;opacity:.28;animation:bsh-spin 46s linear infinite;pointer-events:none}",
        ".bsh-wrap{position:relative;height:100%;box-sizing:border-box;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:max(18px,env(safe-area-inset-top)) 18px max(18px,env(safe-area-inset-bottom));overflow-y:auto}",
        ".bsh-title{font-size:clamp(26px,7.4vw,34px);font-weight:700;letter-spacing:.01em;-webkit-text-stroke:.2em #2A1E3D;paint-order:stroke fill;text-shadow:0 .13em 0 rgba(40,26,64,.85);margin:0 0 14px;text-align:center;animation:bsh-drop .45s cubic-bezier(.2,1.4,.4,1) both}",
        ".bsh-sub{font-size:clamp(14px,3.8vw,16px);color:#BFE0FF;-webkit-text-stroke:.16em #2A1E3D;paint-order:stroke fill;margin:-6px 0 12px;text-align:center}",
        ".bsh-card{display:block;max-height:min(50vh,560px);max-width:78vw;border-radius:20px;box-shadow:0 0 0 5px #2A1E3D,0 12px 0 5px rgba(20,12,40,.55);transform:rotate(-2deg);animation:bsh-pop .55s cubic-bezier(.2,1.5,.35,1) .05s both;-webkit-touch-callout:default}",
        ".bsh-hint{max-width:320px;font-size:13px;line-height:1.3;color:#D8CCFF;opacity:.85;margin:16px 0 0;text-align:center}",
        ".bsh-btns{width:min(88vw,360px);margin-top:16px;display:grid;grid-template-columns:1fr 1fr;gap:10px;animation:bsh-rise .45s cubic-bezier(.2,1.2,.4,1) .15s both}",
        ".bsh-btn{grid-column:span 1;box-sizing:border-box;height:64px;display:flex;align-items:center;justify-content:center;gap:10px;padding:0;margin:0;cursor:pointer;",
        "border-style:solid;border-color:#2A1E3D;border-width:19px 19px 20px 19px;border-image-slice:128 128 132 128 fill;border-image-width:19px 19px 20px 19px;border-image-repeat:stretch;background:transparent;",
        "font:700 clamp(17px,4.8vw,21px) Fredoka,'Arial Rounded MT Bold',system-ui,sans-serif;color:#fff;-webkit-text-stroke:.17em #2A1E3D;paint-order:stroke fill;text-shadow:0 .1em 0 rgba(40,26,64,.7);transition:transform .08s ease-out;white-space:nowrap}",
        ".bsh-btn span{position:relative;top:-3px}",
        ".bsh-btn img{width:34px;height:34px;position:relative;top:-3px;filter:drop-shadow(0 2px 0 rgba(40,26,64,.6))}",
        ".bsh-btn:active{transform:translateY(3px) scale(.97)}",
        ".bsh-wide{grid-column:span 2;height:76px;font-size:clamp(20px,5.8vw,25px)}",
        ".bsh-wide img{width:42px;height:42px}",
        ".bsh-close{position:absolute;top:max(14px,env(safe-area-inset-top));right:14px;width:60px;height:62px;padding:0;cursor:pointer;border-style:solid;border-width:19px 19px 20px 19px;border-image:url(" + UI + "bouton_sombre.png) 128 128 132 128 fill / 19px 19px 20px 19px stretch;background:transparent;display:flex;align-items:center;justify-content:center;animation:bsh-drop .4s ease-out .1s both}",
        ".bsh-close img{width:30px;height:30px;position:relative;top:-3px}",
        ".bsh-close:active{transform:translateY(3px) scale(.95)}",
        ".bsh-done{animation:bsh-punch .3s ease-out}",
        "@keyframes bsh-fade{from{opacity:0}to{opacity:1}}",
        "@keyframes bsh-fadeout{to{opacity:0}}",
        "@keyframes bsh-spin{to{transform:rotate(360deg)}}",
        "@keyframes bsh-pop{0%{opacity:0;transform:rotate(-8deg) scale(.55)}70%{opacity:1;transform:rotate(-1deg) scale(1.04)}100%{transform:rotate(-2deg) scale(1)}}",
        "@keyframes bsh-drop{from{opacity:0;transform:translateY(-18px)}to{opacity:1;transform:none}}",
        "@keyframes bsh-rise{from{opacity:0;transform:translateY(24px)}to{opacity:1;transform:none}}",
        "@keyframes bsh-punch{0%{transform:scale(1)}40%{transform:scale(1.08)}100%{transform:scale(1)}}",
        "@media (prefers-reduced-motion:reduce){.bsh-rays,.bsh-card,.bsh-title,.bsh-btns,.bsh-close{animation:none}}"
      ].join("");
      document.head.appendChild(css);
    }

    var old = document.getElementById("bascule-share");
    if (old && old.parentNode) old.parentNode.removeChild(old);

    var o = document.createElement("div");
    o.id = "bascule-share";
    o.className = "bsh";
    var rays = document.createElement("div");
    rays.className = "bsh-rays";
    o.appendChild(rays);
    var wrap = document.createElement("div");
    wrap.className = "bsh-wrap";
    o.appendChild(wrap);

    var title = document.createElement("div");
    title.className = "bsh-title";
    title.textContent = L.title || "";
    wrap.appendChild(title);

    var blob = null, file = null, url = null;
    if (b64) {
      var bin = atob(b64), arr = new Uint8Array(bin.length);
      for (var i = 0; i < bin.length; i++) arr[i] = bin.charCodeAt(i);
      blob = new Blob([arr], { type: "image/png" });
      try { file = new File([blob], "bascule.png", { type: "image/png" }); } catch (e) { file = null; }
      url = URL.createObjectURL(blob);
      var img = document.createElement("img");
      img.className = "bsh-card";
      img.src = url;
      img.alt = "";
      wrap.appendChild(img);
    } else if (L.sub) {
      var sub = document.createElement("div");
      sub.className = "bsh-sub";
      sub.textContent = text;
      wrap.appendChild(sub);
    }

    var grid = document.createElement("div");
    grid.className = "bsh-btns";
    wrap.appendChild(grid);

    function button(label, color, icon, wide, action) {
      var b = document.createElement("button");
      b.type = "button";
      b.className = "bsh-btn" + (wide ? " bsh-wide" : "");
      b.style.borderImageSource = "url(" + UI + "bouton_" + color + ".png)";
      if (icon) {
        var ic = document.createElement("img");
        ic.src = UI + "icone_" + icon + ".png";
        ic.alt = "";
        ic.onerror = function () { ic.style.display = "none"; };
        b.appendChild(ic);
      }
      var s = document.createElement("span");
      s.textContent = label;
      b.appendChild(s);
      b.addEventListener("click", function (e) { e.stopPropagation(); action(b, s); });
      grid.appendChild(b);
      return b;
    }

    function flash(b, s, label) {
      s.textContent = label;
      b.classList.remove("bsh-done"); void b.offsetWidth; b.classList.add("bsh-done");
    }

    function copy(b, s) {
      function done() { flash(b, s, L.copied || "OK"); }
      function fallback() {
        var t = document.createElement("textarea");
        t.value = text; t.setAttribute("readonly", ""); t.style.cssText = "position:fixed;top:-1000px;opacity:0;";
        document.body.appendChild(t); t.select();
        try { document.execCommand("copy"); done(); } catch (e) {}
        document.body.removeChild(t);
      }
      if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(text).then(done, fallback);
      else fallback();
    }

    function close() {
      o.classList.add("out");
      setTimeout(function () { if (o.parentNode) o.parentNode.removeChild(o); if (url) URL.revokeObjectURL(url); }, 220);
    }

    var canFiles = false;
    try { canFiles = !!(file && navigator.canShare && navigator.canShare({ files: [file] })); } catch (e) {}
    var canShare = canFiles || !!navigator.share;
    if (canShare) {
      button(L.share || "Partager", "bleu", "share", true, function () {
        var data = { text: text };
        if (canFiles) data.files = [file];
        navigator.share(data).catch(function () {});
      });
    }
    if (url) {
      button(L.save || "Enregistrer", "vert", canShare ? null : "share", !canShare, function (b, s) {
        var a = document.createElement("a");
        a.href = url; a.download = "bascule.png";
        document.body.appendChild(a); a.click(); document.body.removeChild(a);
        flash(b, s, L.saved || s.textContent);
      });
    }
    button(L.copy || "Copier", "violet", null, !url || !canShare, copy);

    if (url && L.hint) {
      var h = document.createElement("div");
      h.className = "bsh-hint";
      h.textContent = L.hint;
      wrap.appendChild(h);
    }

    var x = document.createElement("button");
    x.type = "button";
    x.className = "bsh-close";
    x.setAttribute("aria-label", L.close || "Fermer");
    var xi = document.createElement("img");
    xi.src = UI + "icone_close.png"; xi.alt = "";
    x.appendChild(xi);
    x.addEventListener("click", function (e) { e.stopPropagation(); close(); });
    o.appendChild(x);
    o.addEventListener("click", function (e) { if (e.target === o || e.target === wrap) close(); });

    document.body.appendChild(o);
  }
});
