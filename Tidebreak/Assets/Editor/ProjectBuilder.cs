using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Tidebreak.Editor
{
    public static class ProjectBuilder
    {
        [MenuItem("Tidebreak/Prepare game scene")]
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            BuildFont();
            PlayerSettings.companyName="XDwudi";PlayerSettings.productName="Tidebreak";PlayerSettings.bundleVersion="0.6.0";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone,ApiCompatibilityLevel.NET_4_6);
            QualitySettings.antiAliasing=4;QualitySettings.shadowDistance=95;QualitySettings.shadows=ShadowQuality.All;QualitySettings.vSyncCount=1;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Tidebreak · Game bootstrap").AddComponent<GameDirector>();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Tidebreak.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Tidebreak.unity",true)};
            // Keep shaders explicitly referenced because the world is created at runtime.
            Directory.CreateDirectory("Assets/Resources/Materials");
            foreach(string name in new[]{"Standard","Skybox/Procedural"}) {
                string path="Assets/Resources/Materials/"+name.Replace('/','_')+".mat";
                if(!File.Exists(path))AssetDatabase.CreateAsset(new Material(Shader.Find(name)),path);
            }
            AssetDatabase.SaveAssets();Debug.Log("TIDEBREAK_PREPARED");
        }
        static void BuildFont()
        {
            string path="Assets/Resources/Fonts/SeaFont.asset";
            Font source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/SeaText-Regular.otf");
            if(source==null)throw new Exception("Missing redistributable CJK font");
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            bool created=font==null;
            if(created)font=TMP_FontAsset.CreateFontAsset(source,42,5,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            else {font.atlasPopulationMode=AtlasPopulationMode.Dynamic;font.ClearFontAssetData(true);}
            font.name="SeaFont";
            string chars=File.ReadAllText("Assets/Art/Fonts/characters.txt");
            string missing;font.TryAddCharacters(chars,out missing);
            if(!string.IsNullOrEmpty(missing))Debug.LogWarning("Font excluded unsupported control glyphs: "+missing);
            if(created)AssetDatabase.CreateAsset(font,path);
            foreach(var texture in font.atlasTextures)if(!AssetDatabase.Contains(texture))AssetDatabase.AddObjectToAsset(texture,font);
            if(!AssetDatabase.Contains(font.material))AssetDatabase.AddObjectToAsset(font.material,font);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
        }
        [MenuItem("Tidebreak/Build Windows")]
        public static void BuildWindows()
        {
            Prepare();Validate();
            string folder=Path.GetFullPath(Environment.GetEnvironmentVariable("TIDEBREAK_BUILD_OUTPUT")??"../Builds/Release");Directory.CreateDirectory(folder);
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Tidebreak.unity"},locationPathName=Path.Combine(folder,"Tidebreak.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            Directory.CreateDirectory("../Artifacts");
            File.WriteAllText("../Artifacts/build-result.txt",result.summary.result+"\nErrors: "+result.summary.totalErrors+"\nWarnings: "+result.summary.totalWarnings+"\nSize: "+result.summary.totalSize+"\nTime: "+result.summary.totalTime);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Player build failed");
            Debug.Log("TIDEBREAK_BUILD_SUCCEEDED "+result.summary.totalSize);
        }
        [MenuItem("Tidebreak/Validate balance and persistence")]
        public static void Validate()
        {
            int count=0;
            Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception("Validation: "+label);count++;};
            var r=new RunData();
            check(r.MaxHealth==100&&r.DamageMultiplier==1,"baseline stats");
            check(ExpeditionContent.Species.Length==119&&ExpeditionContent.Species.Count(s=>!s.boss)==108,"108 normal species plus 11 bosses");
            check(ExpeditionContent.Species.Select(s=>s.name).Distinct().Count()==119,"unique catalog identities");
            for(int island=1;island<=9;island++){
                r=new RunData{stage=island,maxIsland=island,selectedLure=3};var ids=new System.Collections.Generic.HashSet<int>();var rng=new System.Random(991+island);
                var pool=ExpeditionContent.IslandPool(island,3);
                for(int cast=0;cast<500;cast++){var fish=ExpeditionContent.Roll(r,rng);check(pool.Contains(fish)&&!fish.boss,"catch belongs to this island native/migrant pool");ids.Add(fish.id);}
                check(ids.Count(id=>ExpeditionContent.Species[id].island==island-1)==12,"every local species is catchable with full lure license");
                for(int lure=0;lure<4;lure++){
                    var available=ExpeditionContent.IslandPool(island,lure);check(available.Count(f=>f.island==island-1)/(float)available.Count>.7f,"more than seventy percent native species at every bait tier");
                    check(available.Where(f=>f.endemic).All(f=>f.island==island-1),"endemic species never migrate");
                    for(int other=1;other<=9;other++)if(other!=island)check(available.Select(f=>f.id).Intersect(ExpeditionContent.IslandPool(other,lure).Select(f=>f.id)).Count()/(float)available.Count<.3f,"less than thirty percent species overlap between any two islands");
                }
                r.selectedLure=0;for(int cast=0;cast<50;cast++)check(ExpeditionContent.Roll(r,rng).lure==0,"basic bait does not bypass progression");
                foreach(var offer in ShopCatalog.All)if(offer.island>island)check(ShopCatalog.Lock(r,offer)!="","future gear gate");
            }
            r=new RunData();
            for(int seed=0;seed<250;seed++) {
                var choices=Relic.Roll(new System.Random(seed));check(choices.Distinct().Count()==3,"distinct random upgrades");
            }
            r.criticalRelics=100;r.dodgeRelics=100;r.shieldRelics=100;
            check(r.CriticalChance==.5f&&r.DodgeCooldown==1.6f&&r.DamageTakenMultiplier>=.65f,"defensive caps");
            for(int rod=0;rod<=3;rod++) {
                float progress=0,tension=.15f;
                for(float t=0;t<35&&progress<1;t+=.02f) {
                    bool surge=Mathf.Sin(t*2)>.28f;
                    progress=Mathf.Max(0,progress+Balance.ReelGain(!surge,surge,rod)*.02f);
                    tension=Mathf.Max(0,tension+Balance.TensionGain(!surge,surge,rod)*.02f);
                    check(tension<1,"fishing strategy viable at rod "+rod);
                }
                check(progress>=1,"fishing completes within escape deadline");
            }
            check(Balance.Health(CreatureKind.Kraken,10,false)>Balance.Health(CreatureKind.Leviathan,9,false),"optional boss scaling");
            check(Balance.Bounty(CreatureKind.Snapper,1,true)>Balance.Bounty(CreatureKind.Snapper,1,false),"elite reward incentive");
            string previous=SaveStore.DirectoryOverride;SaveStore.DirectoryOverride=Path.GetFullPath("../Artifacts/SaveValidation");
            try {
                r=new RunData{seed=173,stage=5,coins=219,health=71.2f,shotgun=true,weaponLevel=3};
                SaveStore.Write("voyage",r);var loaded=SaveStore.Read<RunData>("voyage");
                check(loaded.seed==173&&loaded.coins==219&&loaded.shotgun&&loaded.weaponLevel==3,"save roundtrip");
                r.coins=333;SaveStore.Write("voyage",r);
                File.WriteAllText(Path.Combine(SaveStore.DirectoryPath,"voyage.json"),"corrupt");
                loaded=SaveStore.Read<RunData>("voyage");check(loaded!=null&&loaded.coins==219,"corrupt save backup recovery");
                SaveStore.ClearRun();check(SaveStore.Read<RunData>("voyage")==null,"permadeath clears backups");
            } finally {SaveStore.DirectoryOverride=previous;}
            Directory.CreateDirectory("../Artifacts");File.WriteAllText("../Artifacts/validation.txt","PASS: "+count+" assertions\nFishing, relic uniqueness, stat caps, reward scaling, save roundtrip and recovery.");
            // Export the actual runtime definitions for audits and the readable bestiary.
            var rows=new System.Collections.Generic.List<string>{"id,name,island,body,attack,trait,hp,speed,size,tempo,value,lure,boss"};
            foreach(var s in ExpeditionContent.Species)rows.Add(string.Join(",",new[]{s.id.ToString(),s.name,(s.island+1).ToString(),s.body.ToString(),s.attack.ToString(),s.trait.ToString(),s.hp.ToString(System.Globalization.CultureInfo.InvariantCulture),s.speed.ToString(System.Globalization.CultureInfo.InvariantCulture),s.size.ToString(System.Globalization.CultureInfo.InvariantCulture),s.tempo.ToString(System.Globalization.CultureInfo.InvariantCulture),s.value.ToString(),s.lure.ToString(),s.boss?"true":"false"}));
            File.WriteAllLines("../Artifacts/species-catalog.csv",rows,new System.Text.UTF8Encoding(false));
            Debug.Log("TIDEBREAK_VALIDATED "+count);
        }
    }
}
