using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        // Visual fixtures deliberately grant unlocks and choose camera positions. They are
        // presentation checks, not player skill, balance, or commercial performance tests.
        IEnumerator PresentationContracts()
        {
            Time.timeScale = 1;
            var notes = new List<string> {
                "Presentation-only fixtures; synthetic setup, no claim of human playtesting.",
                "Device: " + SystemInfo.deviceModel,
                "OS: " + SystemInfo.operatingSystem,
                "CPU: " + SystemInfo.processorType + " / " + SystemInfo.processorCount + " logical cores",
                "GPU: " + SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType,
                "RAM MB: " + SystemInfo.systemMemorySize + "; VRAM MB: " + SystemInfo.graphicsMemorySize,
                "Unity: " + Application.unityVersion + "; target frame rate: " + Application.targetFrameRate,
                "*-pier files use the captain standing on the actual pier with normal game camera.",
                "*-qa-three-quarter files use a separate inspection camera pose; not gameplay POV."
            };
            foreach (var size in new[] { new Vector2Int(1600, 900), new Vector2Int(1280, 720) })
            {
                Screen.SetResolution(size.x, size.y, false);
                yield return new WaitForSecondsRealtime(.25f);
                string suffix = size.x + "x" + size.y;
                notes.Add("Requested render " + suffix + "; window " + Screen.width + "x" + Screen.height);
                g.StartVoyage(); g.Run.maxIsland = 9; g.Run.coins = 1600;
                g.ShowMap(); g.Travel(5); yield return null;
                g.Run.questStep = 2;
                Guide(); yield return null;
                CapturePresentation("presentation-story-illustration-" + suffix, size.x, size.y);
                g.CloseDialogue(); Shop(); g.UI.ShowBuilds(); yield return null;
                CapturePresentation("presentation-keystone-" + suffix, size.x, size.y);
                g.CloseShop(); g.ShowMap(); yield return null;
                CapturePresentation("presentation-map-" + suffix, size.x, size.y);
                g.CloseShop();
                foreach (int sequence in new[] { 13, 12 })
                {
                    int island = sequence == 13 ? 5 : 9;
                    if (g.Run.stage != island) { g.ShowMap(); g.Travel(island); yield return null; }
                    var cam = g.Player.View; var oldPosition = cam.transform.localPosition;
                    var oldRotation = cam.transform.localRotation; float oldFov = cam.fieldOfView;
                    int completed = 0;
                    g.PlayCinematic(sequence, () => completed++, true);
                    g.SeekCinematicForQA(sequence == 13 ? 9.5f : 16.5f);
                    yield return null;
                    CapturePresentation("presentation-cinematic-" + sequence + "-" + suffix, size.x, size.y);
                    g.SkipCinematic();
                    Check(completed == 1 && !g.CinematicActive &&
                        Vector3.Distance(oldPosition, cam.transform.localPosition) < .001f &&
                        Quaternion.Angle(oldRotation, cam.transform.localRotation) < .01f &&
                        Mathf.Abs(oldFov - cam.fieldOfView) < .01f,
                        "presentation " + suffix + " cinematic " + sequence + " restores camera exactly once");
                }

                foreach (int id in new[] { 111, 115, 116, 117, 118 })
                {
                    Enemy boss = RevisionCreateBoss(id, false);
                    At(new Vector3(0, 0, 23), boss.transform.position + Vector3.up * 1.5f);
                    yield return new WaitForSeconds(.35f);
                    Transform rig = boss.transform.GetChild(0);
                    Vector3 beforeScale = rig.localScale;
                    Quaternion beforeRotation = rig.localRotation;
                    var signature = rig.Find("Boss signature");
                    Check(signature && signature.GetComponent<BossSignatureMotion>(), "boss " + id + " has its animated identity signature");
                    Check(signature && !signature.GetComponentsInChildren<Collider>().Any(c => c.enabled),
                        "boss " + id + " signature geometry cannot intercept shots");
                    yield return new WaitForSeconds(.38f);
                    Check(boss.Motion && (Vector3.Distance(beforeScale, rig.localScale) > .00001f ||
                        Quaternion.Angle(beforeRotation, rig.localRotation) > .001f),
                        "boss " + id + " CreatureMotion changes its living pose at " + suffix);
                    var weak = boss.GetComponentsInChildren<HitRegion>().FirstOrDefault(h => h.GetComponent<Collider>() && h.GetComponent<Collider>().enabled);
                    Check(weak != null, "boss " + id + " retains a shootable weak point");
                    if (weak)
                    {
                        Vector3 target = weak.GetComponent<Collider>().bounds.center;
                        Physics.SyncTransforms(); RaycastHit hit;
                        Vector3 origin = g.Player.View.transform.position;
                        bool visible = Physics.Raycast(origin, (target - origin).normalized, out hit,
                            Vector3.Distance(origin, target) + .1f, ~((1 << 9) | (1 << 30))) && hit.collider.GetComponentInParent<Enemy>() == boss;
                        Check(visible, "boss " + id + " has an unobstructed actual pier sightline at " + suffix);
                        Vector3 front = weak.transform.position + boss.transform.forward * 9;
                        bool frontal = Physics.Raycast(front, (target - front).normalized, out hit, 12,
                            ~((1 << 9) | (1 << 30))) && hit.collider.GetComponent<HitRegion>() == weak;
                        Check(frontal, "boss " + id + " decorations preserve the direct frontal weak point ray");
                    }
                    g.Player.AimAt(boss.transform.position + Vector3.up * (id == 117 ? 2.3f : 1.3f));
                    yield return null;
                    CapturePresentation("presentation-boss-" + id + "-pier-" + suffix, size.x, size.y);

                    // Pause encounter logic through state, retaining animated model components.
                    var camera = g.Player.View; var oldLocal = camera.transform.localPosition;
                    var oldLook = camera.transform.localRotation; float oldLens = camera.fieldOfView;
                    g.SetState(VoyageState.Cinematic);
                    var bounds = new Bounds(boss.transform.position, Vector3.zero);
                    foreach (var renderer in boss.GetComponentsInChildren<Renderer>()) if (renderer.enabled && renderer.gameObject.activeInHierarchy) bounds.Encapsulate(renderer.bounds);
                    float radius = Mathf.Max(4, bounds.extents.magnitude);
                    camera.transform.position = bounds.center + new Vector3(1.05f, .42f, -1.25f).normalized * radius * 3;
                    camera.transform.rotation = Quaternion.LookRotation(bounds.center - camera.transform.position);
                    camera.fieldOfView = 48;
                    yield return null;
                    CapturePresentation("presentation-boss-" + id + "-qa-three-quarter-" + suffix, size.x, size.y);
                    camera.transform.localPosition = oldLocal; camera.transform.localRotation = oldLook; camera.fieldOfView = oldLens;
                    g.SetState(VoyageState.Combat);
                    g.StartVoyage(); yield return null;
                }
            }

            // Small reproducible observation window, not a target-FPS certification.
            Screen.SetResolution(1600, 900, false); yield return new WaitForSecondsRealtime(.3f);
            RevisionCreateBoss(117, false);
            At(new Vector3(0, 0, 23), new Vector3(0, 3, 38));
            yield return new WaitForSecondsRealtime(.4f);
            var frames = new List<float>(); float begin = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - begin < 3) { frames.Add(Time.unscaledDeltaTime * 1000); yield return null; }
            frames.Sort();
            notes.Add("Frame observation: one live Kraken, normal time, actual pier POV, 1600x900, 3 seconds; no screenshot encoding inside sample.");
            if (frames.Count > 0) notes.Add("Samples=" + frames.Count + "; mean_ms=" + frames.Average().ToString("F2") +
                "; median_ms=" + frames[frames.Count / 2].ToString("F2") + "; p95_ms=" + frames[Mathf.Min(frames.Count - 1, (int)(frames.Count * .95f))].ToString("F2"));
            File.WriteAllLines(Path.Combine(output, "presentation-device-and-frametimes.txt"), notes);
            g.StartVoyage(); yield return null;
        }

        void CapturePresentation(string name, int width, int height)
        {
            var camera = g.Player.View;
            var canvas = FindObjectsOfType<Canvas>().First(c => c.name == "Tidebreak UI");
            var mode = canvas.renderMode; var previousCamera = canvas.worldCamera; float plane = canvas.planeDistance;
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .2f;
                camera.targetTexture = rt; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
                Check(texture.width == width && texture.height == height, name + " renders requested native pixel dimensions");
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                canvas.renderMode = mode; canvas.worldCamera = previousCamera; canvas.planeDistance = plane;
                RenderTexture.ReleaseTemporary(rt); Destroy(texture);
            }
        }
    }
}
