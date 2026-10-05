using UnityEngine;

namespace Tidebreak
{
    public partial class BossMechanism
    {
        void DressMechanismTarget(EncounterTarget target)
        {
            if(BossIndex!=5&&BossIndex!=7&&BossIndex!=9)return;
            Transform root=target.transform,core=root.Find("Breakable core");
            if(!core)return;
            // Keep the original animated aiming hull. Replace only its decorative
            // lantern so the object belongs to the animal that exposed it.
            for(int i=root.childCount-1;i>=0;i--){var child=root.GetChild(i);if(child==core)continue;child.gameObject.SetActive(false);Destroy(child.gameObject);}
            for(int i=core.childCount-1;i>=0;i--){var child=core.GetChild(i);child.gameObject.SetActive(false);Destroy(child.gameObject);}
            if(BossIndex==5){
                DressFrostLock(root,core);
            }else if(BossIndex==7){
                Color basalt=new Color(.19f,.115f,.09f),edge=new Color(.57f,.22f,.08f),melt=new Color(1,.58f,.18f);
                CreatureSurfaceArt.Form(core,"Exposed amber seam",Vector3.zero,new Vector3(.78f,.94f,.66f),melt);
                // Four overlapping curved armor leaves frame an open central
                // rupture. Each leaf is a two-sided sculpted surface, not a box.
                for(int side=-1;side<=1;side+=2)for(int row=0;row<2;row++){
                    float y=row*.6f-.48f;
                    CreatureSculpt.Fin(root,"Cooled volcanic armor leaf",new Vector3(side*.3f,y,-.04f),new[]{
                        new Vector3(side*.27f,y-.17f,.06f),new Vector3(side*.83f,y-.22f,.08f),
                        new Vector3(side*1.02f,y+.25f,.13f),new Vector3(side*.7f,y+.68f,.22f),new Vector3(side*.32f,y+.45f,.08f)
                    },basalt,edge,.18f,.075f,false);
                    CreatureSculpt.Curve(root,"Glowing fracture lip",new[]{new Vector3(side*.31f,y-.12f,-.06f),new Vector3(side*.44f,y+.18f,-.12f),new Vector3(side*.33f,y+.43f,-.04f)},new[]{.026f,.043f,.015f},melt,edge,7,4);
                }
                for(int side=-1;side<=1;side+=2)CreatureSculpt.Curve(root,"Charred armor horn",new[]{new Vector3(side*.62f,.45f,.13f),new Vector3(side*.92f,.92f,.18f),new Vector3(side*.81f,1.18f,.27f)},new[]{.16f,.10f,.012f},basalt,edge,9,4);
                CreatureSculpt.Curve(root,"Split forging stem",new[]{new Vector3(0,-1.8f,.35f),new Vector3(-.15f,-1.05f,.3f),new Vector3(0,-.59f,.24f)},new[]{.23f,.27f,.11f},basalt,edge,10,3);
            }else{
                Color skin=new Color(.29f,.095f,.23f),belly=new Color(.74f,.39f,.56f),tendon=new Color(.98f,.7f,.52f);
                CreatureSurfaceArt.Form(core,"Exposed warm tendon",Vector3.zero,new Vector3(.75f,.94f,.66f),tendon);
                for(int side=-1;side<=1;side+=2){
                    CreatureSculpt.Curve(root,"Taut curling wrist",new[]{
                        new Vector3(side*.2f,-1.8f,.25f),new Vector3(side*.72f,-.8f,.17f),new Vector3(side*.82f,.3f,.15f),
                        new Vector3(side*.53f,.88f,.21f),new Vector3(side*.1f,.73f,.28f)
                    },new[]{.24f,.32f,.28f,.15f,.025f},skin,belly,14,5);
                    for(int i=0;i<4;i++){
                        Vector3 at=new Vector3(side*(.65f+Mathf.Sin(i*.7f)*.1f),-.65f+i*.33f,-.1f);
                        CoastalMesh.Ring(root,at,.13f-i*.012f,.03f,belly,Quaternion.Euler(0,side*13,0));
                        CreatureSurfaceArt.Form(root,"Inset sucker",at+Vector3.forward*.015f,new Vector3(.14f,.15f,.055f),skin);
                    }
                }
                CreatureSculpt.Curve(root,"Broken wrist filament",new[]{new Vector3(-.3f,-.42f,-.12f),new Vector3(-.45f,-.05f,-.28f),new Vector3(-.27f,.3f,-.1f)},new[]{.055f,.09f,.02f},tendon,belly,8,5);
            }
            EnemySkillFX.Burst(Owner,target.transform.position,1.1f);
        }

        void DressFrostLock(Transform root,Transform core)
        {
            Color ice=new Color(.49f,.79f,.96f),white=new Color(.91f,.97f,1),blue=new Color(.20f,.53f,.77f);
            CreatureSurfaceArt.Facet(core,"Exposed six-sided frost key",Vector3.zero,new Vector3(.45f,.82f,.45f),new Color(.43f,.93f,1));
            CreatureSurfaceArt.Facet(core,"White heart inside frost key",Vector3.zero,new Vector3(.20f,.92f,.20f),white);

            // A broken, beveled ice shell frames the original aiming hull. Its
            // opening faces the furnace-side approach; no surface spans the hole.
            // CoastalMesh and Curve create visual meshes only, never colliders.
            var frame=new GameObject("Fractured blue ice lock").transform;frame.SetParent(root,false);
            frame.localRotation=Quaternion.LookRotation(new Vector3(-3.5f,0,-1.45f));
            var shell=new CoastalMesh();
            for(int i=0;i<7;i++){
                float a=(i*360f/7+4)*Mathf.Deg2Rad,b=(i*360f/7+45)*Mathf.Deg2Rad,m=(a+b)*.5f;
                float peak=i%3==0?1.08f:.94f;
                Vector3 innerA=new Vector3(Mathf.Cos(a)*.46f,Mathf.Sin(a)*.57f,.06f);
                Vector3 innerB=new Vector3(Mathf.Cos(b)*.46f,Mathf.Sin(b)*.57f,.06f);
                Vector3 outerA=new Vector3(Mathf.Cos(a)*.79f,Mathf.Sin(a)*.89f,0);
                Vector3 outerB=new Vector3(Mathf.Cos(b)*.79f,Mathf.Sin(b)*.89f,0);
                Vector3 tip=new Vector3(Mathf.Cos(m)*peak,Mathf.Sin(m)*(peak+.05f),-.035f);
                Vector3 ridge=new Vector3(Mathf.Cos(m)*.66f,Mathf.Sin(m)*.77f,.19f);
                shell.Tri(innerA,innerB,ridge,white);
                shell.Tri(innerA,ridge,outerA,ice);
                shell.Tri(outerA,ridge,tip,Color.Lerp(white,ice,.3f));
                shell.Tri(tip,ridge,outerB,i%2==0?white:ice);
                shell.Tri(outerB,ridge,innerB,blue);
                Vector3 back=Vector3.back*.19f;
                shell.Quad(innerA,outerA,outerA+back,innerA+back,blue);
                shell.Quad(outerA,tip,tip+back,outerA+back,ice);
                shell.Quad(tip,outerB,outerB+back,tip+back,white);
                shell.Quad(outerB,innerB,innerB+back,outerB+back,ice);
                shell.Quad(innerB,innerA,innerA+back,innerB+back,blue);
                shell.Tri(innerA+back,outerA+back,tip+back,blue);
                shell.Tri(innerA+back,tip+back,innerB+back,ice);
                shell.Tri(innerB+back,tip+back,outerB+back,blue);
            }
            shell.Build("Seven beveled ice petals with open centre",frame);
            for(int side=-1;side<=1;side+=2){
                CreatureSculpt.Curve(frame,"Curving frost talon",new[]{
                    new Vector3(side*.35f,-1.48f,-.05f),new Vector3(side*.72f,-.78f,-.08f),
                    new Vector3(side*.94f,.25f,-.08f),new Vector3(side*.69f,.92f,0),new Vector3(side*.40f,1.22f,.03f)
                },new[]{.08f,.17f,.14f,.085f,.005f},ice,white,6,4);
                CreatureSculpt.Curve(frame,"White melt fracture",new[]{new Vector3(side*.73f,-.27f,.19f),new Vector3(side*.66f,-.02f,.23f),new Vector3(side*.79f,.22f,.16f)},new[]{.009f,.018f,.004f},white,white,5,2);
            }
        }
    }
}
