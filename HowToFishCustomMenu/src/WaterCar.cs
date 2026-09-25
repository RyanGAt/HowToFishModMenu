using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HowToFishCustomMenu
{
    // Amphibious KRAKEN car: a local model you sit in and drive over water and land.
    // Your player is moved with the car through the game's own teleport path, so other
    // players see you gliding along; only KRAKEN users see the car itself.
    public sealed partial class Plugin
    {
        private GameObject car;
        private Transform[] carWheels = new Transform[0];
        private bool driving;
        private float carSpeed, carYaw, carVertical, carBob;
        private const float CarMaxSpeed = 32f, CarBoostSpeed = 65f;

        private void SpawnCar()
        {
            if (!Bridge.Ready) return;
            RemoveCar();
            car = new GameObject("KRAKEN_WaterCar");
            buildRoot = car.transform;
            buildLayer = 0;
            Color paint = Accent, dark = new Color(.05f, .06f, .08f);

            // Body: lower hull, bonnet, boot, cabin with dark windows, spoiler.
            Part(PrimitiveType.Cube, new Vector3(0, .55f, 0), new Vector3(2.2f, .6f, 4.6f), paint, collider: false);
            Part(PrimitiveType.Cube, new Vector3(0, .95f, 1.3f), new Vector3(2.1f, .25f, 1.8f), paint, collider: false);
            Part(PrimitiveType.Cube, new Vector3(0, .95f, -1.6f), new Vector3(2.1f, .25f, 1.2f), paint, collider: false);
            // Open-top convertible so the first-person camera can see out: seats, windscreen, roll bar.
            Part(PrimitiveType.Cube, new Vector3(0, .9f, -.3f), new Vector3(1.8f, .15f, 1.9f), dark, collider: false);
            Part(PrimitiveType.Cube, new Vector3(.45f, 1.15f, -.9f), new Vector3(.7f, .6f, .15f), dark, collider: false);
            Part(PrimitiveType.Cube, new Vector3(-.45f, 1.15f, -.9f), new Vector3(.7f, .6f, .15f), dark, collider: false);
            var screen = Part(PrimitiveType.Cube, new Vector3(0, 1.3f, .45f), new Vector3(1.9f, .5f, .05f), new Color(.6f, .8f, 1f), collider: false);
            screen.transform.localRotation = Quaternion.Euler(-25f, 0, 0);
            Part(PrimitiveType.Cube, new Vector3(0, 1.75f, -1.3f), new Vector3(1.9f, .1f, .12f), paint, emissive: true, collider: false);
            Part(PrimitiveType.Cube, new Vector3(.9f, 1.35f, -1.3f), new Vector3(.1f, .8f, .1f), dark, collider: false);
            Part(PrimitiveType.Cube, new Vector3(-.9f, 1.35f, -1.3f), new Vector3(.1f, .8f, .1f), dark, collider: false);
            Part(PrimitiveType.Cube, new Vector3(0, 1.35f, -2.2f), new Vector3(2.1f, .08f, .5f), paint, collider: false);
            Part(PrimitiveType.Cube, new Vector3(.8f, 1.1f, -2.2f), new Vector3(.08f, .45f, .08f), dark, collider: false);
            Part(PrimitiveType.Cube, new Vector3(-.8f, 1.1f, -2.2f), new Vector3(.08f, .45f, .08f), dark, collider: false);
            // Glowing underglow strip and lights.
            Part(PrimitiveType.Cube, new Vector3(0, .22f, 0), new Vector3(2.3f, .06f, 4.7f), paint, emissive: true, collider: false);
            Part(PrimitiveType.Cube, new Vector3(.7f, .8f, 2.31f), new Vector3(.5f, .18f, .05f), Color.white, emissive: true, collider: false);
            Part(PrimitiveType.Cube, new Vector3(-.7f, .8f, 2.31f), new Vector3(.5f, .18f, .05f), Color.white, emissive: true, collider: false);
            Part(PrimitiveType.Cube, new Vector3(.75f, .8f, -2.31f), new Vector3(.45f, .15f, .05f), Color.red, emissive: true, collider: false);
            Part(PrimitiveType.Cube, new Vector3(-.75f, .8f, -2.31f), new Vector3(.45f, .15f, .05f), Color.red, emissive: true, collider: false);
            // Wheels (cylinders on their side) with glowing hubs.
            var wheels = new System.Collections.Generic.List<Transform>();
            foreach (var w in new[] { new Vector3(1.15f, .45f, 1.5f), new Vector3(-1.15f, .45f, 1.5f), new Vector3(1.15f, .45f, -1.5f), new Vector3(-1.15f, .45f, -1.5f) })
            {
                var pivot = new GameObject("Wheel").transform;
                pivot.SetParent(car.transform, false);
                pivot.localPosition = w;
                var tyre = Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(.9f, .2f, .9f), dark, collider: false);
                tyre.transform.SetParent(pivot, false);
                tyre.transform.localPosition = Vector3.zero;
                tyre.transform.localRotation = Quaternion.Euler(0, 0, 90);
                var hub = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(.42f, .1f, .42f), paint, emissive: true, collider: false);
                hub.transform.SetParent(tyre.transform, false);
                hub.transform.localPosition = new Vector3(0, 1.05f, 0);
                wheels.Add(pivot);
            }
            carWheels = wheels.ToArray();
            var head = new GameObject("Headlight").AddComponent<Light>();
            head.transform.SetParent(car.transform, false);
            head.transform.localPosition = new Vector3(0, .9f, 2.4f);
            head.transform.localRotation = Quaternion.Euler(8, 0, 0);
            head.type = LightType.Spot; head.range = 35f; head.spotAngle = 60f; head.intensity = 3f;

            var look = Bridge.LookTransform ?? Bridge.Player;
            Vector3 fwd = look.forward; fwd.y = 0; if (fwd.sqrMagnitude < .01f) fwd = Vector3.forward;
            carYaw = Quaternion.LookRotation(fwd).eulerAngles.y;
            Vector3 pos = Bridge.Player.position + fwd.normalized * 4f;
            pos.y = SurfaceHeight(pos);
            car.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, carYaw, 0));
            carSpeed = carVertical = 0f;
            Note("KRAKEN CAR SPAWNED // " + keyCar.Value.ToString().ToUpper() + " TO GET IN");
        }

        private void RemoveCar()
        {
            if (driving) ExitCar();
            if (car != null) Destroy(car);
            car = null; carWheels = new Transform[0];
        }
        private void EnterCar()
        {
            if (car == null) SpawnCar();
            if (car == null) return;
            if (fly) StopFly();
            driving = true;
            carSpeed = 0f;
            Bridge.SetYaw(car.transform.eulerAngles.y); // face forward in the seat
            Note("DRIVING // W/S GAS+BRAKE  A/D STEER  SHIFT BOOST  JUMP HOP  " + keyCar.Value.ToString().ToUpper() + " EXIT");
        }
        private void ExitCar()
        {
            driving = false;
            var rb = Bridge.Player != null ? Bridge.Player.GetComponent<Rigidbody>() : null;
            if (rb != null && bodyGravitySaved) rb.useGravity = bodyOldGravity;
            bodyGravitySaved = false;
            if (car != null)
            {
                // Step out onto the roof-level bonnet side; on water you'll swim like normal.
                var p = car.transform.position + car.transform.right * 2.6f;
                p.y = Mathf.Max(p.y, SurfaceHeight(p)) + 1.2f;
                Bridge.Teleport(p);
            }
            Note("Left the car");
        }
        private void ToggleCar() { if (driving) ExitCar(); else EnterCar(); }

        // Water height, or the ground if it's higher (so the car can beach and drive on land).
        private float SurfaceHeight(Vector3 p)
        {
            float water = Bridge.WaterHeight;
            float ground = float.MinValue;
            var hits = Physics.RaycastAll(p + Vector3.up * 30f, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (car != null && h.transform.IsChildOf(car.transform)) continue;
                if (Bridge.Player != null && h.transform.IsChildOf(Bridge.Player)) continue;
                if (h.rigidbody != null && !h.rigidbody.isKinematic) continue; // ignore loose items/fish
                ground = Mathf.Max(ground, h.point.y);
            }
            return Mathf.Max(water, ground);
        }

        private void TickCar()
        {
            if (car == null) return;
            // Spin the wheels even when parked so it looks alive on the water.
            carBob += Time.deltaTime;
            if (!driving)
            {
                var p = car.transform.position;
                p.y = Mathf.Lerp(p.y, SurfaceHeight(p) + Mathf.Sin(carBob * 2f) * .05f, Time.deltaTime * 4f);
                car.transform.position = p;
                return;
            }
            var player = Bridge.Player;
            if (player == null) { driving = false; return; }
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!bodyGravitySaved) { bodyOldGravity = rb.useGravity; bodyGravitySaved = true; }
                rb.useGravity = false; rb.linearVelocity = Vector3.zero;
            }

            float throttle = 0f, steer = 0f;
            bool boost = false, hop = false;
            if (!shown && !ChatOpen())
            {
                if (Input.GetKey(KeyCode.W)) throttle += 1f;
                if (Input.GetKey(KeyCode.S)) throttle -= 1f;
                if (Input.GetKey(KeyCode.D)) steer += 1f;
                if (Input.GetKey(KeyCode.A)) steer -= 1f;
                boost = Input.GetKey(KeyCode.LeftShift);
                hop = Bridge.JumpPressed;
                var pad = Gamepad.current;
                if (pad != null)
                {
                    throttle += pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue();
                    steer += pad.leftStick.x.ReadValue();
                    boost |= pad.leftStickButton.isPressed;
                }
            }
            throttle = Mathf.Clamp(throttle, -1f, 1f); steer = Mathf.Clamp(steer, -1f, 1f);
            float top = boost ? CarBoostSpeed : CarMaxSpeed;
            float target = throttle >= 0 ? throttle * top : throttle * top * .4f;
            carSpeed = Mathf.MoveTowards(carSpeed, target, (Mathf.Abs(target) > Mathf.Abs(carSpeed) ? 22f : 35f) * Time.deltaTime);
            carYaw += steer * Mathf.Lerp(40f, 110f, Mathf.Clamp01(Mathf.Abs(carSpeed) / 15f)) * Mathf.Sign(carSpeed == 0 ? 1 : carSpeed) * Time.deltaTime * (Mathf.Abs(carSpeed) > .5f ? 1f : 0f);

            var t = car.transform;
            Vector3 pos = t.position + Quaternion.Euler(0, carYaw, 0) * Vector3.forward * carSpeed * Time.deltaTime;
            float surface = SurfaceHeight(pos);
            bool grounded = pos.y <= surface + .15f;
            if (grounded)
            {
                if (hop) carVertical = 11f;
                else carVertical = Mathf.Max(carVertical, 0f);
                if (pos.y < surface) pos.y = Mathf.Lerp(pos.y, surface, Time.deltaTime * 12f);
            }
            carVertical -= 25f * Time.deltaTime;
            pos.y += carVertical * Time.deltaTime;
            if (pos.y < surface) { pos.y = surface; carVertical = 0f; }
            bool onWater = surface <= Bridge.WaterHeight + .01f;
            if (onWater && grounded) pos.y += Mathf.Sin(carBob * 3f) * .03f;

            float pitch = Mathf.Clamp(-carVertical * 1.2f, -15f, 15f) - Mathf.Clamp(carSpeed / CarBoostSpeed, -1f, 1f) * (onWater ? 4f : 1f);
            float roll = -steer * Mathf.Clamp01(Mathf.Abs(carSpeed) / 20f) * 6f;
            t.SetPositionAndRotation(pos, Quaternion.Euler(pitch, carYaw, roll));
            foreach (var w in carWheels) w.GetChild(0).Rotate(0, carSpeed * Time.deltaTime * 120f, 0, Space.Self);
            if (carWheels.Length >= 2)
                for (int i = 0; i < 2; i++) carWheels[i].localRotation = Quaternion.Euler(0, steer * 25f, 0);

            // Sit the player in the driver's seat.
            Bridge.Teleport(t.TransformPoint(new Vector3(-.45f, .95f, -.4f)));
        }

        private void DrawSpeedo(Color accent)
        {
            if (!driving) return;
            float kmh = Mathf.Abs(carSpeed) * 3.6f;
            var r = new Rect(Screen.width - 260, Screen.height - 130, 220, 90);
            Rect(r, new Color(0, 0, 0, .6f));
            Rect(new Rect(r.x, r.y, r.width, 4), accent);
            Text(new Rect(r.x, r.y + 6, r.width, 50), kmh.ToString("0"), centre, Color.white);
            Text(new Rect(r.x, r.y + 58, r.width, 24), "KM/H  //  KRAKEN CAR", small, accent);
        }

        private Menu carMenu;
        private Menu CarMenu()
        {
            if (carMenu != null) return carMenu;
            var m = carMenu = new Menu("KRAKEN CAR");
            m.Action("SPAWN CAR", SpawnCar, InWorld, NeedPlayer);
            m.Add(new Option { Kind = OptionKind.Action, NameFn = () => (driving ? "EXIT CAR" : "GET IN / DRIVE") + "  [" + keyCar.Value.ToString().ToUpper() + "]", OnSelect = ToggleCar, Available = InWorld, Requirement = NeedPlayer });
            m.Action("BRING CAR TO ME", () => { if (car == null) SpawnCar(); else { var p = Bridge.InFront(4f); p.y = SurfaceHeight(p); car.transform.position = p; } }, InWorld, NeedPlayer);
            m.Action("REPAINT (MENU COLOUR)", () => { if (car != null) { var keep = driving; var pos = car.transform.position; SpawnCar(); car.transform.position = pos; if (keep) EnterCar(); } });
            m.Action("REMOVE CAR", RemoveCar);
            m.Label("W/S gas+brake, A/D steer, Shift boost, Jump hops.");
            m.Label("Pad: RT gas, LT brake, stick steer.");
            return m;
        }
    }
}
