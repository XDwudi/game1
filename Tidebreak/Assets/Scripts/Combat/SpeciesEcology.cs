using UnityEngine;
namespace Tidebreak
{
    public static partial class SpeciesArt
    {
        static void BuildEcology(Transform root,SpeciesDefinition species)
        {
            float spread=species.body==BodyFamily.Eel?.24f:species.body==BodyFamily.Ray?.9f:.5f;
            Color accent=ExpeditionContent.Islands[species.island].accent;
            for(int side=-1;side<=1;side+=2){
                Vector3 p=new Vector3(side*spread,.33f,-.18f);
                switch(species.island){
                    case 0:break;
                    case 1:
                        Limb(root,"Coral antler",new[]{p,p+new Vector3(side*.1f,.4f,-.15f),p+new Vector3(side*.27f,.65f,-.35f)},.07f);
                        Limb(root,"Coral branch",new[]{p+Vector3.up*.25f,p+new Vector3(-side*.14f,.48f,.15f)},.045f);
                        Part(root,"Coral tip",p+new Vector3(side*.27f,.65f,-.35f),Vector3.one*.12f,new Color(1,.61f,.62f));break;
                    case 2:
                        Part(root,"Translucent poison sac",p,new Vector3(.22f,.35f,.5f),new Color(.65f,.85f,.24f));
                        Limb(root,"Root tendril",new[]{p,p+new Vector3(side*.2f,-.15f,-.6f),p+new Vector3(side*.35f,.08f,-1.05f)},.045f);break;
                    case 3:
                        for(int j=0;j<3;j++){var plate=Part(root,"Saltglass armor",p+Vector3.back*j*.22f,new Vector3(.22f,.14f,.3f),new Color(.8f,.65f,.38f),PrimitiveType.Cube);plate.localRotation=Quaternion.Euler(0,0,side*32);}
                        break;
                    case 4:
                        var plank=Part(root,"Wreckage armor",p+Vector3.up*.04f,new Vector3(.17f,.14f,.95f),new Color(.29f,.3f,.24f),PrimitiveType.Cube);plank.localRotation=Quaternion.Euler(0,side*12,side*28);
                        for(int j=0;j<3;j++)CoastalMesh.Ring(root,p+new Vector3(side*.12f,-j*.12f,-.1f),.09f,.023f,new Color(.48f,.31f,.16f),Quaternion.Euler(0,j%2*90,0));break;
                    case 5:
                        for(int j=0;j<3;j++){var shard=Part(root,"Ice dorsal shard",p+Vector3.back*j*.25f,new Vector3(.09f,.38f+j*.12f,.18f),new Color(.58f,.9f,1),PrimitiveType.Cube);shard.localRotation=Quaternion.Euler(23,0,-side*28);}
                        break;
                    case 6:
                        Shape.Beam(root,p,p+new Vector3(side*.16f,.6f,-.2f),.035f,new Color(.25f,.27f,.35f));
                        Shape.Part("Electric lure",PrimitiveType.Sphere,root,p+new Vector3(side*.16f,.6f,-.2f),Vector3.one*.15f,new Color(.57f,.8f,1),false,true);
                        CoastalMesh.Ring(root,p+Vector3.up*.2f,.17f,.025f,accent,Quaternion.Euler(90,0,0));break;
                    case 7:
                        for(int j=0;j<3;j++){var plate=Part(root,"Volcanic shell",p+Vector3.back*j*.22f,new Vector3(.31f,.18f,.29f),new Color(.17f,.13f,.16f),PrimitiveType.Cube);plate.localRotation=Quaternion.Euler(j*8,0,side*25);Shape.Part("Molten seam",PrimitiveType.Cube,root,p+new Vector3(0,.105f,-j*.22f),new Vector3(.19f,.025f,.03f),new Color(1,.3f,.07f),false,true);}break;
                    case 8:
                        for(int j=0;j<2;j++){var mirror=Part(root,"Mirror scale",p+new Vector3(side*.22f,j*.24f,-j*.4f),new Vector3(.12f,.32f,.27f),new Color(.6f,.63f,.94f),PrimitiveType.Cube);mirror.localRotation=Quaternion.Euler(15,side*40,side*25);}
                        break;
                }
            }
        }
    }
}
