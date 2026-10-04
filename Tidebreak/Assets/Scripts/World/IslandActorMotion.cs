using UnityEngine;

namespace Tidebreak
{
    // Individually articulated guide. Feet stay planted; breathing, gaze and gestures have
    // different rhythms so dialogue does not look like a rigid model rocking side to side.
    public sealed class IslandActorMotion : MonoBehaviour
    {
        public Transform Head, Chest, LeftArm, RightArm, LeftForearm, RightForearm, Book, Mouth;
        public Transform[] Eyes;
        public bool Speaking;
        Vector3 chestScale, bookPosition, mouthScale;
        float phase, gesture, gazeYaw, gazePitch;

        public void Initialize(int island)
        {
            phase = island * .73f;
            if (Chest) chestScale = Chest.localScale;
            if (Book) bookPosition = Book.localPosition;
            if (Mouth) mouthScale = Mouth.localScale;
        }

        void LateUpdate()
        {
            var game = GameDirector.Instance;
            if (!game || game.Paused) return;
            float time = Time.time + phase;
            bool talking = Speaking || game.State == VoyageState.Dialogue;
            gesture = Mathf.MoveTowards(gesture, talking ? 1 : 0, Time.deltaTime * 2.4f);
            Vector3 observer = game.Player ? game.Player.View.transform.position : transform.position + transform.forward * 5;
            Vector3 local = transform.InverseTransformPoint(observer) - Vector3.up * 1.7f;
            bool near = local.sqrMagnitude < 120 && local.z > -.5f;
            float targetYaw = near ? Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -38, 38) : Mathf.Sin(time * .27f) * 9;
            float targetPitch = near ? Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -16, 21) : 11;
            gazeYaw = Mathf.LerpAngle(gazeYaw, targetYaw, Time.deltaTime * 4);
            gazePitch = Mathf.LerpAngle(gazePitch, targetPitch, Time.deltaTime * 3);
            if (Head) Head.localRotation = Quaternion.Euler(gazePitch + Mathf.Sin(time * 5.2f) * gesture * 1.6f, gazeYaw, Mathf.Sin(time * .7f) * 1.2f);
            if (Chest) Chest.localScale = Vector3.Scale(chestScale, new Vector3(1 + Mathf.Sin(time * 1.8f) * .012f, 1 + Mathf.Sin(time * 1.8f) * .017f, 1));

            // The notebook arm remains supported; the free palm opens on an occasional phrase.
            float phrase = Mathf.Pow(Mathf.Max(0, Mathf.Sin(time * 1.35f)), 2) * gesture;
            if (LeftArm) LeftArm.localRotation = Quaternion.Euler(-15 - phrase * 24, -8, -9 - phrase * 18);
            if (LeftForearm) LeftForearm.localRotation = Quaternion.Euler(-14 - phrase * 28, 0, 0);
            if (RightArm) RightArm.localRotation = Quaternion.Euler(-24 + Mathf.Sin(time * 1.8f) * 1.2f, 6, 12);
            if (RightForearm) RightForearm.localRotation = Quaternion.Euler(-48 + Mathf.Sin(time * .9f) * 2, 0, 0);
            if (Book) { Book.localPosition = bookPosition + Vector3.up * Mathf.Sin(time * 1.8f) * .006f; Book.localRotation = Quaternion.Euler(-10, -10 + Mathf.Sin(time * .4f) * 2, 0); }
            if (Mouth) Mouth.localScale = Vector3.Scale(mouthScale, new Vector3(1, 1 + Mathf.Abs(Mathf.Sin(time * 12)) * gesture * 1.1f, 1));
            float blink = Mathf.Repeat(time, 4.7f);
            if (Eyes != null) foreach (var eye in Eyes) if (eye)
                eye.localScale = new Vector3(.066f, blink > 4.52f ? .009f : .066f, .055f);
        }
    }
}
