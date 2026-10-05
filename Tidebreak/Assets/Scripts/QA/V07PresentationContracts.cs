using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        IEnumerator V07Contracts()
        {
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Artifacts/V07QA",V05Arg("-v07Run","manual")));
            Directory.CreateDirectory(output);Time.timeScale=1;
            string mode=V05Arg("-v07Mode","All");
            File.WriteAllText(Path.Combine(output,"scope.txt"),"Visual fixtures: preconfigured money/unlocks, model inspection cameras and scripted effect stress. Not a natural progression or human playtest. UI tests inspect actual TMP glyph geometry and screen bounds. Gameplay regressions are executed separately.\n");
            if(mode=="All"||mode=="UI")yield return V07ShopContracts();
            if(mode=="All"||mode=="Models")yield return V07ModelContracts();
            if(mode=="All"||mode=="Effects")yield return V07EffectContracts();
            g.StartVoyage();Time.timeScale=1;yield return null;
        }
        bool V07VisibleText(TMP_Text text)
        {
            text.ForceMeshUpdate();bool visible=true;int digits=0;
            for(int i=0;i<text.textInfo.characterCount;i++){
                var c=text.textInfo.characterInfo[i];if(!char.IsDigit(c.character))continue;
                digits++;visible&=c.isVisible&&c.topRight.y>c.bottomLeft.y&&c.topRight.x>c.bottomLeft.x;
            }
            var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
            var canvas=text.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            foreach(var p in corners){Vector2 screen=RectTransformUtility.WorldToScreenPoint(camera,p);visible&=screen.x>=-1&&screen.x<=Screen.width+1&&screen.y>=-1&&screen.y<=Screen.height+1;}
            return visible&&digits>0&&!text.isTextOverflowing;
        }
        // Called only after WaitForEndOfFrame: inspect the actual overlay framebuffer,
        // without moving the UI next to the camera's near clip plane for a capture.
        void CaptureV07Screen(string name,int width,int height)
        {
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            try{
                bool content=false;
                for(int y=16;y<texture.height;y+=32)for(int x=16;x<texture.width;x+=32)content|=texture.GetPixel(x,y).maxColorComponent>.08f;
                if(!content){
                    File.AppendAllText(Path.Combine(output,"capture-methods.txt"),name+": hidden-window backbuffer was black; separate world and overlay cameras used.\n");
                    CapturePresentation(name,width,height,V07PricePixels);return;
                }
                V07PricePixels(texture);
                File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
                File.AppendAllText(Path.Combine(output,"capture-methods.txt"),name+": native overlay framebuffer.\n");
                Check(texture.width==width&&texture.height==height,name+" captures the native overlay framebuffer at requested dimensions");
            }
            finally{Destroy(texture);}
        }
        void V07PricePixels(Texture2D texture)
        {
            foreach(var label in FindObjectsOfType<TMP_Text>().Where(t=>t.isActiveAndEnabled&&t.name.StartsWith("Shop price ")&&t.text.Any(char.IsDigit))){
                var canvas=label.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);
                Vector2 a=RectTransformUtility.WorldToScreenPoint(camera,corners[0]),b=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
                int ink=0;
                for(int y=Mathf.Max(0,Mathf.CeilToInt(a.y)+3);y<Mathf.Min(texture.height,Mathf.FloorToInt(b.y)-3);y++)
                    for(int x=Mathf.Max(0,Mathf.CeilToInt(a.x)+2);x<Mathf.Min(texture.width,Mathf.FloorToInt(b.x)-2);x++){
                        Color pixel=texture.GetPixel(x,y);if(pixel.r>.55f&&pixel.g>.48f&&pixel.b>.30f)ink++;
                    }
                Check(ink>12,"v07 rendered price region contains readable light glyph pixels: "+label.name+" / "+texture.width+"x"+texture.height);
            }
        }
        IEnumerator V07ShopContracts()
        {
            string[] tabs={"枪械与配件","钓具与拟饵","防护与补给","本岛随机库存","流派专精","岛屿专研"};
            foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1280,720),new Vector2Int(1280,1024)}){
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);
                g.StartVoyage();g.Run.coins=1600;Shop();yield return null;
                for(int tab=0;tab<tabs.Length;tab++){
                    Check(CampaignClick(tabs[tab]),"v07 opens actual workshop category "+tab+" at "+size);yield return null;Canvas.ForceUpdateCanvases();
                    var prices=FindObjectsOfType<TMP_Text>().Where(t=>t.isActiveAndEnabled&&t.name.StartsWith("Shop price ")).ToArray();
                    Check(prices.Length>0,"v07 category "+tab+" contains independent visible coin prices at "+size);
                    foreach(var price in prices){price.ForceMeshUpdate();Check(price.text.Any(char.IsDigit)?V07VisibleText(price):!string.IsNullOrEmpty(price.text)&&!price.isTextOverflowing,"v07 amount or owned-state glyphs fit and render: "+price.name+" / "+size);}
                    if(tab<3)foreach(var offer in ShopCatalog.All.Where(o=>o.category==tab)){
                        var label=prices.FirstOrDefault(t=>t.name=="Shop price "+offer.id);int price=offer.Price(g.Run);
                        if(price>=0)Check(label&&label.text.Contains(price.ToString()),"v07 catalogue amount remains visible for locked/available "+offer.id+" at "+size);
                    }
                    yield return new WaitForEndOfFrame();CaptureV07Screen("shop-category-"+tab+"-"+size.x+"x"+size.y,size.x,size.y);
                }
                CampaignClick(tabs[0]);Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
                int oldCoins=g.Run.coins,oldLevel=g.Run.weaponLevel;
                var purchase=FindObjectsOfType<UnityEngine.UI.Button>().FirstOrDefault(b=>b.isActiveAndEnabled&&b.interactable&&b.name=="Shop purchase weapon");
                Check(purchase,"v07 real weapon offer remains an enabled button at "+size);
                if(purchase){
                    var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,((RectTransform)purchase.transform).TransformPoint(((RectTransform)purchase.transform).rect.center))};
                    var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                    var graphic=purchase.targetGraphic;
                    File.AppendAllText(Path.Combine(output,"ui-raycasts.txt"),size+" pointer="+pointer.position+" depth="+graphic.depth+" culled="+graphic.canvasRenderer.cull+" canvas="+graphic.canvas.renderMode+" hits="+string.Join(",",hits.Take(8).Select(h=>h.gameObject.name).ToArray())+"\n");
                    Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==purchase,"v07 decorative graphics do not intercept the price/purchase bar at "+size);
                    purchase.onClick.Invoke();yield return null;
                    Check(g.Run.coins==oldCoins-85&&g.Run.weaponLevel==oldLevel+1,"v07 clicking the priced card deducts exactly 85 coins at "+size);
                }
                g.Run.coins=0;g.UI.ShowShop();yield return null;CampaignClick(tabs[0]);yield return null;
                var reload=FindObjectsOfType<TMP_Text>().FirstOrDefault(t=>t.isActiveAndEnabled&&t.name=="Shop price reload");
                Check(reload&&reload.text.Contains("55")&&V07VisibleText(reload),"v07 insufficient funds keeps exact cost readable at "+size);
                yield return new WaitForEndOfFrame();CaptureV07Screen("shop-insufficient-funds-"+size.x+"x"+size.y,size.x,size.y);
                g.Run.weaponLevel=5;g.Run.maxIsland=9;g.Run.coins=9999;g.UI.ShowShop();yield return null;CampaignClick(tabs[0]);yield return null;
                purchase=FindObjectsOfType<UnityEngine.UI.Button>().FirstOrDefault(b=>b.isActiveAndEnabled&&b.name=="Shop purchase weapon");
                Check(purchase&&!purchase.interactable,"v07 fully upgraded offer cannot be bought again at "+size);
                yield return new WaitForEndOfFrame();CaptureV07Screen("shop-owned-and-maxed-"+size.x+"x"+size.y,size.x,size.y);
                g.CloseShop();
            }
        }
        IEnumerator V07ModelContracts()
        {
            Screen.SetResolution(1600,900,false);yield return new WaitForSecondsRealtime(.2f);g.StartVoyage();yield return null;
            foreach(int island in new[]{1,6,7}){
                if(island!=1){g.Run.maxIsland=9;g.ShowMap();g.Travel(island);yield return null;}
                var actors=FindObjectsOfType<IslandActorMotion>();Check(actors.Length==3,"v07 guide and two merchants have articulated models on island "+island);
                foreach(var actor in actors){
                    var pieces=actor.GetComponentsInChildren<MeshFilter>();
                    Check(pieces.Length>=8&&pieces.All(m=>m.sharedMesh&&m.sharedMesh.vertexCount>0),"v07 tailored model has valid articulated meshes: "+actor.name);
                    Check(!actor.transform.IsChildOf(g.World.Scenery),"v07 animated actor remains outside static scenery batch: "+actor.name);
                    At(actor.transform.position+actor.transform.forward*3.5f,actor.transform.position+Vector3.up*1.1f);yield return new WaitForSeconds(.15f);
                    var camera=g.Player.View;Vector3 local=camera.transform.localPosition;Quaternion rotation=camera.transform.localRotation;float lens=camera.fieldOfView;
                    camera.transform.position=actor.transform.TransformPoint(new Vector3(.85f,1.27f,3.3f));camera.transform.rotation=Quaternion.LookRotation(actor.transform.position+Vector3.up*1.1f-camera.transform.position);camera.fieldOfView=36;
                    var actorCanvas=FindObjectsOfType<Canvas>().First(c=>c.name=="Tidebreak UI");var heldTools=camera.transform.Find("Handheld tools");
                    actorCanvas.enabled=false;heldTools.gameObject.SetActive(false);
                    CapturePresentation("actor-"+island+"-"+actor.name.Replace(' ','-'),1600,900);
                    actorCanvas.enabled=true;heldTools.gameObject.SetActive(true);
                    camera.transform.localPosition=local;camera.transform.localRotation=rotation;camera.fieldOfView=lens;
                }
            }
            g.StartVoyage();yield return null;
            var view=g.Player.View;Vector3 oldPosition=view.transform.localPosition;Quaternion oldRotation=view.transform.localRotation;float oldFov=view.fieldOfView;var flags=view.clearFlags;Color background=view.backgroundColor;
            var canvas=FindObjectsOfType<Canvas>().First(c=>c.name=="Tidebreak UI");
            var inspectionTools=view.transform.Find("Handheld tools");inspectionTools.gameObject.SetActive(false);
            foreach(int id in Enumerable.Range(0,12).Concat(new[]{108,113,117,118})){
                var studio=new GameObject("v07 model inspection studio");studio.transform.position=new Vector3(0,600,0);
                var model=SpeciesArt.Build(studio.transform,ExpeditionContent.Species[id],id<12);
                var renderers=model.GetComponentsInChildren<MeshRenderer>();Bounds bounds=new Bounds(model.position,Vector3.zero);foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                Check(renderers.Length>3&&bounds.size.sqrMagnitude>.1f,"v07 sculpted anatomy renders for species "+id);
                var weak=model.GetComponentsInChildren<HitRegion>().FirstOrDefault();Check(weak,"v07 sculpture retains weak organ for species "+id);
                view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.075f,.11f,.13f);view.fieldOfView=35;
                float distance=Mathf.Max(2,bounds.extents.magnitude)*3.2f;view.transform.position=bounds.center+new Vector3(1,.35f,1.45f).normalized*distance;view.transform.rotation=Quaternion.LookRotation(bounds.center-view.transform.position);
                canvas.enabled=false;CapturePresentation("sculpted-species-"+id+"-inspection",1600,900);canvas.enabled=true;
                Destroy(studio);yield return null;
            }
            inspectionTools.gameObject.SetActive(true);view.transform.localPosition=oldPosition;view.transform.localRotation=oldRotation;view.fieldOfView=oldFov;view.clearFlags=flags;view.backgroundColor=background;
            g.StartVoyage();yield return null;
        }
        IEnumerator V07EffectContracts()
        {
            Screen.SetResolution(1600,900,false);yield return new WaitForSecondsRealtime(.2f);g.StartVoyage();yield return null;
            g.Run.shotgun=g.Run.harpoon=g.Run.carbine=g.Run.burstRifle=g.Run.arcCaster=true;
            foreach(var weapon in new[]{WeaponKind.Revolver,WeaponKind.Scattergun,WeaponKind.Harpoon,WeaponKind.Carbine,WeaponKind.BurstRifle,WeaponKind.ArcCaster}){
                g.SetState(VoyageState.Combat);At(new Vector3(0,0,15),new Vector3(0,2,29));g.Player.Equip(weapon);g.Player.Refill();yield return new WaitForSeconds(.7f);
                int before=g.Feedback.Emitted;bool fired=g.Player.Fire();yield return new WaitForEndOfFrame();
                Check(fired&&g.Feedback.Emitted>before,"v07 real Fire emits layered feedback for "+weapon);
                CapturePresentation("effect-muzzle-"+weapon,1600,900);yield return new WaitForSeconds(.7f);
            }
            Vector3 target=new Vector3(0,2.5f,21);At(new Vector3(0,0,15),target);
            g.Feedback.Impact(target,Vector3.back,true,true,true);yield return new WaitForEndOfFrame();CapturePresentation("effect-weak-kill-early",1600,900);
            yield return new WaitForSeconds(.1f);CapturePresentation("effect-weak-kill-decay",1600,900);
            g.Feedback.Splash(new Vector3(0,-.35f,29));g.Player.AimAt(new Vector3(0,-.35f,29));yield return new WaitForSeconds(.08f);CapturePresentation("effect-splash-crown",1600,900);
            // Warm all six weapon variants, then verify repeated bursts do not add emitters/renderers.
            yield return new WaitForSeconds(1);
            int emitters=g.Feedback.GetComponentsInChildren<ParticleSystem>(true).Length;
            int rendererCount=g.Feedback.GetComponentsInChildren<Renderer>(true).Length;
            for(int n=0;n<180;n++){g.Feedback.Impact(target,Vector3.back,true,n%3==0,n%5==0);g.Feedback.Tracer(target,target+Vector3.right*6,Color.cyan);if(n%12==0)yield return null;}
            Check(g.Feedback.GetComponentsInChildren<ParticleSystem>(true).Length==emitters&&g.Feedback.GetComponentsInChildren<Renderer>(true).Length==rendererCount,"v07 effect stress reuses emitters and renderers after warmup");
            g.Feedback.Clear();yield return null;
            Check(g.Feedback.GetComponentsInChildren<ParticleSystem>(true).All(p=>p.particleCount==0),"v07 clear removes active particles on voyage reset");
            Check(g.Feedback.ActiveAccents==0&&g.Feedback.ActiveTracers==0,"v07 clear hides mesh accents and tracer pool on voyage reset");
            g.StartVoyage();yield return null;
        }
    }
}
