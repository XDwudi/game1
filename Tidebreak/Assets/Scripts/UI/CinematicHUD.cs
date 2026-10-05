using TMPro;
using UnityEngine;

namespace Tidebreak
{
    public partial class SeaHUD
    {
        RectTransform cinemaRoot;
        TextMeshProUGUI cinemaChapter, cinemaSpeaker, cinemaSubtitle, cinemaDocument;
        UnityEngine.UI.Image cinemaFade, cinemaProgress, cinemaDocumentBack;

        public void BeginCinematicHUD()
        {
            ClearModal();
            hud.gameObject.SetActive(false);
            if (!cinemaRoot)
            {
                // The bars stretch across the physical canvas; text stays inside the safe area.
                cinemaRoot = Full("Cinematic presentation", root.parent);
                var content = Rect("Cinematic safe area", cinemaRoot, 0, 0, 1600, 900);
                content.anchorMin = content.anchorMax = content.pivot = Vector2.one * .5f;
                content.anchoredPosition = Vector2.zero;
                var top = Box(cinemaRoot, 0, 0, 1600, 110, Color.black).rectTransform;
                top.anchorMin = new Vector2(0, 1); top.anchorMax = Vector2.one;
                top.sizeDelta = new Vector2(0, 110); top.anchoredPosition = Vector2.zero;
                var bottom = Box(cinemaRoot, 0, 0, 1600, 148, Color.black).rectTransform;
                bottom.anchorMin = Vector2.zero; bottom.anchorMax = new Vector2(1, 0);
                bottom.pivot = Vector2.zero; bottom.sizeDelta = new Vector2(0, 148);
                bottom.anchoredPosition = Vector2.zero;
                content.SetAsLastSibling();
                Text(content, 74, 33, 340, 22, "T I D E B R E A K  /  航海纪事", 14, Muted);
                cinemaChapter = Text(content, 74, 62, 1090, 38, "", 23, Cream);
                Text(content, 1210, 43, 315, 34, "SPACE / ESC  跳过演出", 15, Muted, TextAlignmentOptions.Right);
                cinemaSpeaker = Text(content, 190, 772, 1220, 28, "", 18, Gold, TextAlignmentOptions.Center);
                cinemaSubtitle = Text(content, 150, 811, 1300, 65, "", 25, Cream, TextAlignmentOptions.Center);
                cinemaSubtitle.lineSpacing = 8;
                cinemaDocumentBack = Plate(content, 1006, 235, 470, 350, Paper,true);
                Stitch(cinemaDocumentBack.transform,25,18,420,new Color(PaperInk.r,PaperInk.g,PaperInk.b,.30f));
                cinemaDocument = Text(cinemaDocumentBack.transform, 25, 32, 420, 300, "", 24, PaperInk);
                cinemaDocument.lineSpacing = 14;
                cinemaDocument.fontStyle = FontStyles.Italic;
                cinemaProgress = Box(content, 76, 897, 1448, 3, Mint);
                cinemaProgress.type = UnityEngine.UI.Image.Type.Filled;
                cinemaProgress.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                var fadeRect = Full("Cinematic dissolve", cinemaRoot);
                cinemaFade = fadeRect.gameObject.AddComponent<UnityEngine.UI.Image>();
                cinemaFade.color = Color.black; cinemaFade.raycastTarget = false;
            }
            cinemaRoot.gameObject.SetActive(true);
            cinemaRoot.SetAsLastSibling();
            cinemaFade.color = Color.black;
        }

        public void UpdateCinematicHUD(string chapter, string speaker, string subtitle, string document,
            float progress, float fade)
        {
            if (!cinemaRoot) return;
            cinemaChapter.text = chapter;
            cinemaSpeaker.text = speaker;
            cinemaSubtitle.text = subtitle;
            cinemaDocument.text = document;
            cinemaDocumentBack.gameObject.SetActive(!string.IsNullOrEmpty(document));
            cinemaProgress.fillAmount = Mathf.Clamp01(progress);
            cinemaFade.color = new Color(0, 0, 0, Mathf.Clamp01(fade));
        }

        public void EndCinematicHUD()
        {
            if (cinemaRoot) cinemaRoot.gameObject.SetActive(false);
        }
    }
}
