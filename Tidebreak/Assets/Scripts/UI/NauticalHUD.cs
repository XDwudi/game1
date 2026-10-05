using System;
using TMPro;
using UnityEngine;

namespace Tidebreak
{
    public partial class SeaHUD
    {
        static readonly Color Paper=new Color(.91f,.87f,.76f), PaperInk=new Color(.105f,.23f,.265f), PaperMuted=new Color(.30f,.39f,.38f), Brass=new Color(.77f,.60f,.34f);
        NauticalPlateGraphic Plate(Transform p,float x,float y,float w,float h,Color tint,bool paper=false,string name="Nautical panel")
        {
            var at=Rect(name,p,x,y,w,h);var graphic=at.gameObject.AddComponent<NauticalPlateGraphic>();graphic.color=tint;graphic.Paper=paper;graphic.raycastTarget=false;return graphic;
        }
        NauticalGraphic Icon(Transform p,float x,float y,float size,NauticalMark mark,Color tint,string name="Nautical illustration")
        {
            var at=Rect(name,p,x,y,size,size);var graphic=at.gameObject.AddComponent<NauticalGraphic>();graphic.Mark=mark;graphic.color=tint;graphic.raycastTarget=false;return graphic;
        }
        void Stitch(Transform p,float x,float y,float width,Color tint)
        {for(float at=0;at<width-4;at+=11)Box(p,x+at,y,4,1,tint);}
        void DecorateMenu(RectTransform p)
        {
            // A faint engraved chart lives behind the content; it has no raycast target.
            Icon(p,1138,-138,603,NauticalMark.Compass,new Color(.49f,.66f,.64f,.055f));
            Icon(p,1330,730,240,NauticalMark.Wave,new Color(.65f,.63f,.43f,.045f));
            Box(p,42,45,1,809,new Color(Brass.r,Brass.g,Brass.b,.20f));
            for(int y=70;y<842;y+=24)Box(p,38,y,y%48==22?8:4,1,new Color(Brass.r,Brass.g,Brass.b,.23f));
        }
        void WorkshopHeader(RectTransform p)
        {
            var r=game.Run;
            Icon(p,80,52,35,NauticalMark.Anchor,Gold);
            Text(p,128,56,890,27,"PINEHAVEN OUTFITTERS  /  群岛补给社",14,Gold).characterSpacing=1.2f;
            Text(p,76,100,1100,64,"为下一片海，做好准备",43,Cream);
            Text(p,80,178,1038,31,game.Island.name+" · 金币工坊    /    装备许可证 "+r.maxIsland+" / 9",19,Muted);
            Text(p,80,222,1000,25,"新岛屿开放新商品 · 标本解锁专研 · 所有成长均用金币购买",15,Muted);
            var wallet=Plate(p,1208,70,312,121,Paper,true,"Workshop wallet");
            Icon(wallet.transform,18,40,49,NauticalMark.Coin,PaperInk);
            Text(wallet.transform,83,16,205,25,"船长金币",14,PaperMuted);
            Text(wallet.transform,81,46,207,51,r.coins.ToString("N0"),32,PaperInk,TextAlignmentOptions.Left).fontStyle=FontStyles.Bold;
            Text(p,1215,208,298,33,"把发现，变成下一次优势",15,Gold,TextAlignmentOptions.Right);
            Stitch(p,80,252,1440,new Color(Brass.r,Brass.g,Brass.b,.52f));
        }
        void StockCard(RectTransform p,float x,float y,ShopOffer item)
        {
            const float w=346,h=154;var r=game.Run;
            string locked=ShopCatalog.Lock(r,item);int actual=game.Price(item.id),catalogue=item.Price(r);
            bool can=locked==""&&actual>=0&&r.coins>=actual;
            Plate(p,x+3,y+4,w,h,new Color(0,0,0,.22f));
            Plate(p,x,y,w,h,Paper,true,"Shop offer "+item.id);
            Icon(p,x+292,y+9,35,ItemMark(item.id),new Color(PaperInk.r,PaperInk.g,PaperInk.b,.82f),"Shop illustration "+item.id);
            // Title and purchase keep the same x anchor for controller/test navigation.
            var title=Text(p,x+16,y+9,270,31,item.name+(item.max<10&&item.max>1?" "+item.level(r)+" / "+item.max:""),19,PaperInk);
            title.gameObject.name="Shop title "+item.id;title.fontStyle=FontStyles.Bold;
            title.enableWordWrapping=false;title.overflowMode=TextOverflowModes.Ellipsis;
            var description=Text(p,x+16,y+43,314,43,item.Description(r),14,PaperMuted);description.lineSpacing=1;
            string state=locked!=""?locked:actual<0?"已入库 · 无需再次购买":can?"可立即装配": "还需 "+Mathf.Max(0,actual-r.coins)+" 金币";
            var availability=Text(p,x+16,y+87,314,20,state,12,locked==""?PaperInk:new Color(.43f,.35f,.23f));
            availability.gameObject.name="Shop requirement "+item.id;availability.enableWordWrapping=false;
            // A locked gate is a separate sentence, never a replacement for the price.
            int shownPrice=locked!=""?catalogue:actual;
            PurchaseStrip(p,x+16,y+111,314,38,item.id,shownPrice,locked!=""?"待解锁":actual<0?"已拥有 / 已满":can?"购买":"金币不足",()=>game.Buy(item.id),can);
        }
        void PurchaseStrip(Transform parent,float x,float y,float w,float h,string id,int price,string actionLabel,Action action,bool available)
        {
            h=Mathf.Max(38,h);
            var plate=Plate(parent,x,y,w,h,available?new Color(.10f,.30f,.31f):new Color(.135f,.20f,.22f),false,"Shop purchase "+id);
            plate.Cut=7;plate.raycastTarget=true;
            var button=plate.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=plate;button.interactable=available;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.24f,1.24f,1.12f);colors.pressedColor=new Color(.73f,.84f,.80f);colors.disabledColor=Color.white;button.colors=colors;
            Icon(plate.transform,10,(h-26)*.5f,26,price>=0?NauticalMark.Coin:NauticalMark.Seal,price>=0?Gold:Muted);
            string label=price>=0?price+" 金币 · "+actionLabel:actionLabel;
            var amount=Text(plate.transform,44,2,w-56,h-4,label,17,available?Cream:price>=0?new Color(.91f,.79f,.60f):Muted,TextAlignmentOptions.MidlineLeft);
            amount.gameObject.name="Shop price "+id;
            // SeaFont lineHeight/pointSize is 1.448: 20 pt in a 26 px box previously
            // ellipsized the entire price. Fixed single-line 17 pt in >=34 px is safe.
            amount.enableWordWrapping=false;amount.overflowMode=TextOverflowModes.Overflow;
            button.onClick.AddListener(()=>{game.Audio.Cue("select");action();});
        }
        static NauticalMark ItemMark(string id)
        {
            switch(id){
                case "weapon":case "brake":case "handling":return NauticalMark.Wrench;
                case "shotgun":case "carbine":case "burst":return NauticalMark.Rifle;
                case "harpoon":return NauticalMark.Harpoon;case "arc":return NauticalMark.Arc;
                case "scope":case "precision":return NauticalMark.Sonar;case "reload":return NauticalMark.Revolver;
                case "rod":case "bearing":return NauticalMark.Reel;case "bag":case "salvage":return NauticalMark.Bag;
                case "lure1":case "lure2":case "lure3":case "bait":return NauticalMark.Lure;case "chum":return NauticalMark.Fish;
                case "hull":return NauticalMark.Coat;case "heal":return NauticalMark.Soup;
                case "medkit":case "dressing":return NauticalMark.Medicine;case "bomb":return NauticalMark.Grenade;
                case "frost":return NauticalMark.Snow;case "sonar":return NauticalMark.Sonar;
                case "boots":return NauticalMark.Boot;case "tonic":return NauticalMark.Flask;
                default:return NauticalMark.Seal;
            }
        }
    }
}
