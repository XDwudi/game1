using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Silhouettes are authored in each anatomy's local space. The caller supplies the
    // existing rig, retaining its scale (ordinary bosses ~3.3, Kraken 1, white whale 4.4).
    // These ornaments have no hit colliders and leave the original frontal weak spot clear.
    public static class BossIdentityArt
    {
        static readonly Color Iron = new Color(.12f, .19f, .22f);
        static readonly Color Bronze = new Color(.66f, .43f, .19f);
        static readonly Color Bone = new Color(.89f, .83f, .62f);
        static readonly Color Ice = new Color(.56f, .85f, .98f);

        public static void BuildSignature(Transform rig, SpeciesDefinition species)
        {
            if (!rig || !species.boss || species.id < 108 || species.id > 118 || rig.Find("Boss signature")) return;
            Transform signature = Pivot(rig, "Boss signature", Vector3.zero);
            var motion = signature.gameObject.AddComponent<BossSignatureMotion>();
            switch (species.id)
            {
                case 108: WreckCrown(signature, motion); break;
                case 109: CoralAntlers(signature, motion); break;
                case 110: ToxicRoots(signature, motion); break;
                case 111: WalkingTemple(signature, motion); break;
                case 112: SunkenBell(signature, motion); break;
                case 113: FrostCrown(signature, motion, rig); break;
                case 114: StormArray(signature, motion); break;
                case 115: FurnaceLobster(signature, motion); break;
                case 116: MirrorHalo(signature, motion); break;
                case 117: AncientEye(signature, motion, rig); break;
                case 118: WhaleSonar(signature, motion); break;
            }
        }

        static Transform Pivot(Transform parent, string name, Vector3 at)
        {
            var p = new GameObject(name).transform;
            p.SetParent(parent, false); p.localPosition = at;
            return p;
        }

        static Transform Solid(Transform parent, string name, Vector3 at, Vector3 size, Color color,
            PrimitiveType shape = PrimitiveType.Sphere, bool glow = false)
        { return Shape.Part(name, shape, parent, at, size, color, false, glow).transform; }

        static Transform Tube(Transform parent, string name, Vector3[] points, float[] radii, Color color)
        { return CoastalMesh.Tube(name, parent, points, radii, color, Color.Lerp(color, Bone, .22f), 7); }

        static Transform Ring(Transform parent, string name, Vector3 at, float radius, float thickness,
            Color color, Quaternion rotation)
        {
            // Ring's vertices are normally baked at `at`; a joint keeps orbital motion local.
            var p = Pivot(parent, name, at);
            CoastalMesh.Ring(p, Vector3.zero, radius, thickness, color, rotation);
            return p;
        }

        static Transform Shard(Transform parent, string name, Vector3 at, Vector3 size, Color color)
        {
            var m = new CoastalMesh();
            Vector3[] edge = { Vector3.right * .5f, Vector3.forward * .35f,
                Vector3.left * .5f, Vector3.back * .35f };
            for (int i = 0; i < 4; i++)
            {
                var a = Vector3.Scale(edge[i], size); var b = Vector3.Scale(edge[(i + 1) % 4], size);
                m.Tri(a, Vector3.up * size.y, b, Color.Lerp(color, Color.white, i * .08f));
                m.Tri(a, b, Vector3.down * size.y * .24f, color * .7f);
            }
            var piece = m.Build(name, parent).transform; piece.localPosition = at;
            return piece;
        }

        static void WreckCrown(Transform root, BossSignatureMotion motion)
        {
            Color wood = new Color(.37f, .22f, .12f);
            var wheel = Pivot(root, "Broken helm crown", new Vector3(0, .76f, -.36f));
            wheel.localRotation = Quaternion.Euler(-8, 0, -13);
            Ring(wheel, "Ship wheel rim", Vector3.zero, .56f, .063f, wood, Quaternion.identity);
            Ring(wheel, "Rusted helm band", Vector3.zero, .49f, .021f, Bronze, Quaternion.identity);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Vector3 d = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                float length = i == 5 ? .44f : .72f;
                Shape.Beam(wheel, d * .09f, d * length, .038f, wood);
                if (i != 5) Solid(wheel, "Helm grip", d * length, new Vector3(.086f, .12f, .07f), Bronze);
            }
            Solid(wheel, "Helm hub", Vector3.zero, new Vector3(.2f, .2f, .13f), Bronze);
            motion.Sway(wheel, Vector3.forward, 3, .65f);
            for (int side = -1; side <= 1; side += 2)
            {
                var plate = Pivot(root, "Salvaged pincer armour", new Vector3(side * .81f, .25f, .99f));
                var armor = Shape.Rock(plate, Vector3.zero, new Vector3(.48f, .27f, .52f), Iron, 6);
                armor.name = "Riveted iron carapace";
                Shape.Beam(plate, new Vector3(-.18f, .16f, -.17f), new Vector3(.18f, .16f, .17f), .022f, Bronze);
                for (int i = -1; i <= 1; i += 2) Solid(plate, "Rivet", new Vector3(i * .14f, .16f, i * .12f), Vector3.one * .045f, Bone);
                motion.Sway(plate, Vector3.up, 3, 1.25f, side);
            }
        }

        static void CoralAntlers(Transform root, BossSignatureMotion motion)
        {
            Color coral = new Color(.97f, .47f, .38f), tip = new Color(1, .77f, .61f);
            for (int side = -1; side <= 1; side += 2)
            {
                var branch = Pivot(root, "Royal branching coral", new Vector3(side * .55f, .14f, -.21f));
                Tube(branch, "Coral main stem", new[] { Vector3.zero, new Vector3(side * .31f, .35f, -.05f),
                    new Vector3(side * .57f, .77f, -.18f), new Vector3(side * .6f, 1.24f, -.24f) }, new[] { .14f, .115f, .075f, .02f }, coral);
                for (int fork = 0; fork < 3; fork++)
                {
                    float y = .25f + fork * .28f;
                    Tube(branch, "Coral fork", new[] { new Vector3(side * (.17f + fork * .14f), y, -.03f),
                        new Vector3(side * (.55f + fork * .16f), y + .12f, .02f),
                        new Vector3(side * (.66f + fork * .17f), y + .48f, -.04f) }, new[] { .075f, .06f, .012f }, tip);
                }
                Solid(branch, "Sleeping coral pearl", new Vector3(side * .32f, .46f, .13f), Vector3.one * .15f, new Color(.96f, .79f, .34f), PrimitiveType.Sphere, true);
                motion.Sway(branch, Vector3.forward, 3, 1.15f, side * .7f);
                Ring(root, "Wing resonance ridge", new Vector3(side * 1.13f, .05f, -.35f), .24f, .04f, Bone, Quaternion.Euler(70, side * 20, 0));
            }
        }

        static void ToxicRoots(Transform root, BossSignatureMotion motion)
        {
            Color bark = new Color(.2f, .29f, .11f), poison = new Color(.55f, .84f, .19f);
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 4; i++)
            {
                float z = -1.45f + i * .62f;
                var rootlet = Pivot(root, "Mangrove root mane", new Vector3(side * .17f, .07f, z));
                Tube(rootlet, "Crooked root", new[] { Vector3.zero, new Vector3(side * .28f, .32f, -.15f),
                    new Vector3(side * .72f, .58f + i * .05f, -.36f), new Vector3(side * .91f, .42f, -.62f) }, new[] { .075f, .066f, .045f, .006f }, bark);
                Tube(rootlet, "Root barb", new[] { new Vector3(side * .28f, .32f, -.15f),
                    new Vector3(side * .36f, .76f, -.31f), new Vector3(side * .3f, .96f, -.39f) }, new[] { .045f, .021f, .004f }, bark);
                motion.Sway(rootlet, Vector3.up, 6, 1.5f, i * .65f + side);
                var sac = Solid(root, "Luminous venom bladder", new Vector3(side * .29f, -.09f, z + .08f),
                    new Vector3(.19f, .34f, .26f), poison, PrimitiveType.Sphere, true);
                motion.Pulse(sac, .1f, 2, i * .55f);
            }
        }

        static void WalkingTemple(Transform root, BossSignatureMotion motion)
        {
            Color stone = new Color(.61f, .55f, .38f), worn = new Color(.83f, .76f, .53f);
            var temple = Pivot(root, "Burden of the sun temple", new Vector3(0, .44f, -.23f));
            Solid(temple, "Temple foundation", new Vector3(0, .05f, 0), new Vector3(1.18f, .15f, 1.25f), stone, PrimitiveType.Cube);
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 at = new Vector3(x * .41f, .16f, z * .42f);
                var m = new CoastalMesh(); m.Cone(at, .09f, .5f, worn, 6, .065f); m.Build("Six sided temple pillar", temple);
                Solid(temple, "Pillar capital", at + Vector3.up * .5f, new Vector3(.24f, .075f, .24f), stone, PrimitiveType.Cube);
            }
            Solid(temple, "Temple lintel", new Vector3(0, .74f, 0), new Vector3(1.12f, .14f, 1.18f), worn, PrimitiveType.Cube);
            var roof = new CoastalMesh();
            roof.Tri(new Vector3(-.66f, .81f, -.64f), new Vector3(0, 1.06f, -.64f), new Vector3(.66f, .81f, -.64f), stone);
            roof.Tri(new Vector3(.66f, .81f, .64f), new Vector3(0, 1.06f, .64f), new Vector3(-.66f, .81f, .64f), worn);
            roof.Quad(new Vector3(-.66f, .81f, -.64f), new Vector3(-.66f, .81f, .64f), new Vector3(0, 1.06f, .64f), new Vector3(0, 1.06f, -.64f), stone);
            roof.Quad(new Vector3(0, 1.06f, -.64f), new Vector3(0, 1.06f, .64f), new Vector3(.66f, .81f, .64f), new Vector3(.66f, .81f, -.64f), worn);
            roof.Build("Sun temple pediment", temple);
            var sun = Ring(temple, "Floating solar dial", new Vector3(0, 1.36f, -.13f), .37f, .045f, Bronze, Quaternion.identity);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4; Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                Shape.Beam(sun, d * .3f, d * .49f, .022f, Bone);
            }
            Solid(sun, "Sun heart", Vector3.zero, Vector3.one * .18f, new Color(.95f, .67f, .24f), PrimitiveType.Sphere, true);
            motion.Orbit(sun, Vector3.forward, 8);
        }

        static void SunkenBell(Transform root, BossSignatureMotion motion)
        {
            var bell = Pivot(root, "Midnight ship bell", new Vector3(0, .62f, -.55f));
            var mesh = new CoastalMesh(); mesh.Cone(Vector3.zero, .45f, .58f, Bronze, 12, .21f); mesh.Build("Bronze bell skirt", bell);
            Ring(bell, "Bell lip", Vector3.zero, .45f, .055f, Bone, Quaternion.Euler(90, 0, 0));
            Solid(bell, "Bell shoulder", Vector3.up * .58f, new Vector3(.44f, .18f, .44f), Bronze);
            Ring(bell, "Bell suspension", Vector3.up * .76f, .12f, .029f, Iron, Quaternion.identity);
            Shape.Beam(bell, Vector3.up * .2f, Vector3.down * .14f, .038f, Iron);
            Solid(bell, "Bell tongue", Vector3.down * .14f, Vector3.one * .13f, Bone);
            motion.Sway(bell, Vector3.right, 10, 1.15f);
            for (int i = 0; i < 2; i++)
            {
                var band = Ring(root, "Forged mantle hoop", new Vector3(0, .06f, -.75f + i * .6f), .49f, .055f, Iron, Quaternion.identity);
                band.localScale = new Vector3(1, 1.23f, 1);
                for (int side = -1; side <= 1; side += 2)
                    Solid(root, "Iron hoop bolt", new Vector3(side * .46f, .23f, -.75f + i * .6f), Vector3.one * .085f, Bronze);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                var chain = Pivot(root, "Trailing anchor chain", new Vector3(side * .44f, -.1f, -.63f));
                for (int link = 0; link < 5; link++) Ring(chain, "Anchor chain link", new Vector3(side * link * .045f, -link * .13f, -.03f * link), .077f, .018f, Iron, Quaternion.Euler(0, link % 2 * 90, 0));
                motion.Sway(chain, Vector3.forward, 7, 1.1f, side);
            }
        }

        static void FrostCrown(Transform root, BossSignatureMotion motion, Transform anatomy)
        {
            // Replace the generic sword bill with one continuous icy tusk below the weak spot.
            var oldSword = anatomy.Find("Sword"); if (oldSword) oldSword.gameObject.SetActive(false);
            Tube(root, "Ancient ice tusk", new[] { new Vector3(0, -.025f, .92f), new Vector3(0, .01f, 1.55f),
                new Vector3(0, .1f, 2.4f), new Vector3(0, .19f, 2.95f) }, new[] { .082f, .06f, .027f, .002f }, Ice);
            for (int i = -2; i <= 2; i++)
            {
                var crown = Pivot(root, "Frost crown blade", new Vector3(i * .18f, .4f, -.34f + Mathf.Abs(i) * .09f));
                crown.localRotation = Quaternion.Euler(-12, 0, -i * 16);
                Shard(crown, "Blue ice crown", Vector3.zero, new Vector3(.28f, .88f - Mathf.Abs(i) * .15f, .32f), Ice);
                motion.Sway(crown, Vector3.forward, 1.5f, 1, i * .6f);
            }
            for (int side = -1; side <= 1; side += 2)
                Tube(root, "Frost ridge", new[] { new Vector3(side * .3f, .2f, .45f), new Vector3(side * .38f, .29f, -.2f),
                    new Vector3(side * .19f, .19f, -.9f) }, new[] { .04f, .07f, .015f }, Bone);
        }

        static void StormArray(Transform root, BossSignatureMotion motion)
        {
            Color steel = new Color(.26f, .32f, .44f), power = new Color(.76f, .61f, 1);
            for (int side = -1; side <= 1; side += 2)
            {
                var aerial = Pivot(root, "Storm aerial", new Vector3(side * .42f, .66f, -.13f));
                Tube(aerial, "Tuned antenna spine", new[] { Vector3.zero, new Vector3(side * .18f, .41f, 0),
                    new Vector3(side * .13f, .87f, 0), new Vector3(side * .33f, 1.12f, 0) }, new[] { .067f, .048f, .028f, .008f }, steel);
                for (int ring = 0; ring < 3; ring++) Ring(aerial, "Antenna induction coil", new Vector3(side * .16f, .33f + ring * .2f, 0), .15f - ring * .025f, .021f, power, Quaternion.Euler(90, 0, 0));
                var capacitor = Solid(aerial, "Charged capacitor", new Vector3(side * .33f, 1.12f, 0), Vector3.one * .14f, power, PrimitiveType.Sphere, true);
                motion.Pulse(capacitor, .2f, 3, side);
                motion.Sway(aerial, Vector3.forward, 2.5f, 1.4f, side);
            }
            var array = Ring(root, "Orbiting induction array", new Vector3(0, .68f, -.08f), .87f, .028f, steel, Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3;
                Solid(array, "Array contact", new Vector3(Mathf.Cos(a) * .87f, 0, Mathf.Sin(a) * .87f), Vector3.one * .12f, power, PrimitiveType.Sphere, true);
            }
            motion.Orbit(array, Vector3.up, 17);
        }

        static void FurnaceLobster(Transform root, BossSignatureMotion motion)
        {
            Color basalt = new Color(.23f, .15f, .14f), lava = new Color(1, .35f, .1f);
            Transform segment = Pivot(root, "Articulated lobster tail", new Vector3(0, .08f, -.43f));
            for (int i = 0; i < 6; i++)
            {
                segment = Pivot(segment, "Tail segment joint " + i, new Vector3(0, i > 2 ? .09f : -.02f, -.32f));
                float width = .82f - i * .085f;
                var shell = Shape.Rock(segment, Vector3.zero, new Vector3(width, .24f, .45f), basalt, 8); shell.name = "Obsidian tail plate";
                Ring(segment, "Molten segment seam", new Vector3(0, .025f, -.2f), width * .34f, .022f, lava, Quaternion.identity);
                motion.Sway(segment, Vector3.right, 3.5f, 1.35f, i * -.6f);
            }
            var fan = new CoastalMesh();
            for (int i = -2; i <= 2; i++)
            {
                Vector3 tip = new Vector3(i * .23f, .02f, -.75f + Mathf.Abs(i) * .15f);
                fan.Tri(Vector3.zero, tip + Vector3.right * .15f, tip - Vector3.right * .15f, Color.Lerp(basalt, lava, .25f));
                fan.Tri(Vector3.zero, tip - Vector3.right * .15f, tip + Vector3.right * .15f, basalt);
                Shape.Beam(segment, Vector3.zero, tip, .024f, lava);
            }
            fan.Build("Armoured lobster tail fan", segment);
            var furnace = Pivot(root, "Living furnace carapace", new Vector3(0, .49f, -.19f));
            Shape.Rock(furnace, Vector3.zero, new Vector3(.72f, .48f, .73f), basalt, 8);
            for (int side = -1; side <= 1; side += 2)
            {
                var stack = new CoastalMesh(); stack.Cone(new Vector3(side * .28f, .16f, -.18f), .12f, .63f, Iron, 7, .1f);
                stack.Build("Furnace chimney", furnace);
                Ring(furnace, "Chimney mouth", new Vector3(side * .28f, .81f, -.18f), .106f, .025f, lava, Quaternion.Euler(90, 0, 0));
                for (int vent = 0; vent < 3; vent++)
                {
                    var slit = Solid(furnace, "Carapace heat vent", new Vector3(side * .48f, .1f + vent * .105f, -.04f),
                        new Vector3(.04f, .036f, .41f - vent * .05f), lava, PrimitiveType.Cube, true);
                    motion.Pulse(slit, .12f, 2.3f, vent * .6f);
                }
            }
        }

        static void MirrorHalo(Transform root, BossSignatureMotion motion)
        {
            Color silver = new Color(.68f, .9f, .91f), voidBlue = new Color(.17f, .31f, .43f);
            var halo = Pivot(root, "Broken mirror constellation", new Vector3(0, .15f, -.56f));
            Ring(halo, "Incomplete star orbit", Vector3.zero, 1.07f, .018f, silver, Quaternion.identity);
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2 / 7;
                var plate = Pivot(halo, "Orbiting mirror shard", new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 1.06f);
                plate.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90);
                Shard(plate, "Obsidian mirror frame", Vector3.zero, new Vector3(.38f, .58f, .12f), voidBlue);
                Shard(plate, "Silver mirror face", new Vector3(0, .04f, .03f), new Vector3(.27f, .43f, .1f), silver);
                motion.Sway(plate, Vector3.up, 13, .7f, i * .6f);
            }
            motion.Orbit(halo, Vector3.forward, 9);
            for (int side = -1; side <= 1; side += 2)
                Tube(root, "Reflected gill seam", new[] { new Vector3(side * .34f, .19f, .56f), new Vector3(side * .45f, 0, .43f),
                    new Vector3(side * .32f, -.23f, .35f) }, new[] { .025f, .038f, .015f }, silver);
        }

        static void AncientEye(Transform root, BossSignatureMotion motion, Transform anatomy)
        {
            Color gold = new Color(.92f, .66f, .26f), rune = new Color(.69f, .42f, .9f);
            var eye = Pivot(root, "Eye above the abyss", new Vector3(0, 2.2f, 1.88f));
            Solid(eye, "Ancient eye socket", Vector3.zero, new Vector3(1.48f, 1.4f, .31f), new Color(.2f, .1f, .24f));
            Solid(eye, "Unblinking iris", Vector3.forward * .15f, new Vector3(1.15f, 1.07f, .12f), gold, PrimitiveType.Sphere, true);
            var pupil = Solid(eye, "Vertical pupil", Vector3.forward * .23f, new Vector3(.23f, .84f, .055f), new Color(.025f, .012f, .04f));
            Ring(eye, "Eye scar rim", Vector3.forward * .2f, .63f, .038f, Bronze, Quaternion.identity);
            motion.Pulse(pupil, .075f, 1.25f);
            for (int i = 0; i < 8; i++)
            {
                var arm = anatomy.Find("Tentacle " + i); if (!arm) continue;
                var mark = Pivot(arm, "Ancient arm seal", new Vector3(3.8f, .76f, .25f));
                mark.localRotation = Quaternion.Euler(75, 0, -12);
                Ring(mark, "Arm seal circle", Vector3.zero, .26f, .025f, rune, Quaternion.identity);
                Shape.Beam(mark, new Vector3(-.13f, 0, 0), new Vector3(0, .23f, 0), .027f, rune, true);
                Shape.Beam(mark, new Vector3(0, .23f, 0), new Vector3(.13f, 0, 0), .027f, rune, true);
                Shape.Beam(mark, new Vector3(0, .15f, 0), new Vector3(0, -.2f, 0), .027f, rune, true);
                motion.Pulse(mark, .055f, 1.8f, i * .45f);
            }
        }

        static void WhaleSonar(Transform root, BossSignatureMotion motion)
        {
            Color scar = new Color(.34f, .61f, .81f);
            for (int side = -1; side <= 1; side += 2)
            for (int cut = 0; cut < 3; cut++)
            {
                float z = .38f - cut * .44f;
                Tube(root, "Blue glacier scar", new[] { new Vector3(side * .37f, .45f, z + .2f),
                    new Vector3(side * .57f, .14f, z + .07f), new Vector3(side * .53f, -.08f, z - .02f),
                    new Vector3(side * .36f, -.36f, z - .19f) }, new[] { .024f, .037f, .018f, .006f }, scar);
            }
            var crown = Pivot(root, "Forehead sonar crest", new Vector3(0, .59f, .63f));
            for (int i = 0; i < 4; i++)
            {
                var fin = Shard(crown, "Sonar ice ridge", new Vector3(0, -.055f * i, -i * .29f),
                    new Vector3(.24f + i * .025f, .49f - i * .07f, .4f), Ice);
                motion.Pulse(fin, .018f, 1.65f, i * .4f);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                var ice = Shard(root, "Old shoulder ice", new Vector3(side * .47f, .27f, .8f), new Vector3(.16f, .28f, .25f), Ice);
                ice.localRotation = Quaternion.Euler(9, 0, side * -32);
            }
        }
    }

    // Small movements are isolated to authored pivots. No per-frame mesh allocations.
    public sealed class BossSignatureMotion : MonoBehaviour
    {
        sealed class Joint
        {
            public Transform transform;
            public Vector3 axis, scale;
            public Quaternion rotation;
            public float amount, rate, phase;
            public int mode;
        }
        readonly List<Joint> joints = new List<Joint>();
        Enemy owner;
        public void Sway(Transform target, Vector3 axis, float degrees, float speed, float phase = 0)
        { Add(target, axis, degrees, speed, phase, 0); }
        public void Orbit(Transform target, Vector3 axis, float degreesPerSecond)
        { Add(target, axis, 1, degreesPerSecond, 0, 1); }
        public void Pulse(Transform target, float amplitude, float speed, float phase = 0)
        { Add(target, Vector3.zero, amplitude, speed, phase, 2); }
        void Add(Transform target, Vector3 axis, float amount, float speed, float phase, int mode)
        {
            joints.Add(new Joint { transform = target, axis = axis, amount = amount, rate = speed,
                phase = phase, mode = mode, rotation = target.localRotation, scale = target.localScale });
        }
        void Start() { owner = GetComponentInParent<Enemy>(); }
        void LateUpdate()
        {
            if (owner && owner.game && owner.game.Paused) return;
            float time = Time.unscaledTime;
            foreach (var joint in joints)
            {
                if (!joint.transform) continue;
                if (joint.mode == 2) joint.transform.localScale = joint.scale * (1 + Mathf.Sin(time * joint.rate + joint.phase) * joint.amount);
                else joint.transform.localRotation = joint.rotation * Quaternion.AngleAxis(joint.mode == 1 ? time * joint.rate : Mathf.Sin(time * joint.rate + joint.phase) * joint.amount, joint.axis);
            }
        }
    }
}
