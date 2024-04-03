using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using PixelGunCheat.util;
using Steamworks;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;
using Renderer = PixelGunCheat.util.Renderer;

namespace PixelGunCheat
{
    public class CheatManager : MonoBehaviour
    {
        private long tickCount = 0;
        private GameController gameController;
        private List<Player_move_c> playerList = new();
        private Player_move_c player;
        private bool _initMat = false;
        private ManualLogSource logger = Logger.CreateLogSource("Cheat");
        private Vector3 aimedPos = Vector3.zero;
        private Dictionary<string, Vector3> oldPos = new();

        private void Awake()
        {
            Debug.Log("Loaded Cheat");
            gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnDestroy()
        {
            Debug.Log("Destroyed Cheat");
        }

        private void Update()
        {
            tickCount++;
            if (tickCount % 5 == 0)
            {
                gameController = FindObjectOfType<GameController>();
                playerList = FindObjectsOfType<Player_move_c>().ToList();
                player = playerList.Find(p => p.nickLabel.text == "1111");
            }
            if (gameController == null) return;
            if (player == null) return;
            player.MaxBackpackAmmo();
            
            // Ammo and Reloads
            WeaponSounds weaponSounds = FindObjectsOfType<WeaponSounds>().ToList().Find(s => s.prop_Player_move_c_0.nickLabel.text == player.nickLabel.text);
            if (weaponSounds != null)
            {
                // No Recoil
                weaponSounds.recoilCoeff = 0;
                weaponSounds.recoilCoeffZoom = 0;

                // Max Speed + Full Auto
                weaponSounds.shootDelay = 0.000001f;
                weaponSounds.bulletDelay = 0.000001f;
                weaponSounds.delayInBurstShooting = 0.000001f;
                weaponSounds.chargeTime = 0.000001f;

                // Instant ADS and XRAY
                weaponSounds.scopeSpeed = 0;
                weaponSounds.zoomXray = true;

                // Unlimited Ammo
                ItemRecord itemRecord = weaponSounds.prop_ItemRecord_0;
                if (itemRecord != null)
                {
                    itemRecord.isUnlimitedAmmo = true;
                    itemRecord.modulesClipAmmoModifier = 99999;
                }
            }

            Camera main = Camera.main;
            if (main == null) return;
            
            // Drop TP
            foreach (var coinBonus in FindObjectsOfType<CoinBonus>())
            {
                coinBonus.transform.position = main.transform.position;
            }
            foreach (var armorBonus in FindObjectsOfType<ArmorBonus>())
            {
                armorBonus.transform.position = main.transform.position;
            }
            foreach (var itemBonus in FindObjectsOfType<BonusItem>())
            {
                itemBonus.transform.position = main.transform.position;
            }
            foreach (var weaponBonus in FindObjectsOfType<WeaponBonus>())
            {
                weaponBonus.transform.position = main.transform.position;
            }

            Vector3 aimPos = main.transform.position;
            Vector3 prediction = Vector3.zero;
            Player_move_c target = null;
            double distance = double.MaxValue;
            foreach (var p in playerList)
            {
                if(p.nickLabel.text == "1111") continue;
                if(!(p.nickLabel.color.r == 1 && p.nickLabel.color.g == 0 && p.nickLabel.color.b == 0)) continue;
                if(!PlayerUtil.IsVisible(PlayerUtil.WorldToScreenPoint(main, p.transform.position))) continue;

                Vector3 velocity = Vector3.zero;
                if (oldPos.ContainsKey(p.nickLabel.text))
                {
                    velocity = p.transform.position - oldPos[p.nickLabel.text];
                }

                oldPos[p.nickLabel.text] = p.transform.position;

                Vector3 headPos = p.transform.position + new Vector3(0, 0.75f, 0) + (velocity.normalized / 5);
                
                if(Vector3.Distance(headPos, aimPos) > 600) continue;

                Vector3 aimDirection = main.transform.rotation * new Vector3(1, 1, 1);
                Vector3 v = headPos - aimPos;
                float d = Vector3.Dot(v, aimDirection);
                Vector3 closestPoint = aimPos + aimDirection * d;

                float newDist = Vector3.Distance(closestPoint, headPos);
                if (!(distance > newDist)) continue;

                RaycastHit hit;
                if (Physics.Raycast(new Ray(aimPos, Vector3.Normalize(headPos - aimPos)), out hit, 600))
                {
                    if (hit.colliderInstanceID != p.headCollider.GetInstanceID()) continue;
                }
                else
                {
                    continue;
                }

                distance = newDist;
                target = p;
                prediction = velocity.normalized / 5;
            }

            if (target != null)
            {
                main.transform.LookAt(target.transform.position + new Vector3(0, 0.75f, 0) + prediction);
                aimedPos = target.transform.position + new Vector3(0, 0.75f, 0) + prediction;
            }
            else
            {
                aimedPos = Vector3.zero;
            }
        }

        private void OnGUI()
        {
            if (!_initMat)
            {
                Renderer.InitMat();
                _initMat = true;
            }
            Renderer.DrawString(new Vector2(26, 26), "PixelGunCheat :3", Color.black, false);
            Renderer.DrawString(new Vector2(25, 25), "PixelGunCheat :3", Color.cyan, false);
            Renderer.DrawString(new Vector2(26, 101), "github.com/stanuwu", Color.black, false);
            Renderer.DrawString(new Vector2(25, 100), "github.com/stanuwu", Color.cyan, false);
            if (gameController == null) return;
            Camera main = Camera.main;
            if (main == null) return;
            foreach (var playerMoveC in playerList.OrderByDescending(p => Vector3.Distance(p.transform.position, main.transform.position)))
            {
                Vector3 position = playerMoveC.transform.position;
                Vector3 topWorld = position + new Vector3(0, 2, 0);
                
                Vector3 screenPos = PlayerUtil.WorldToScreenPoint(main, position);
                Vector3 topScreen = PlayerUtil.WorldToScreenPoint(main, topWorld);

                if(screenPos.z < 0) continue;
                
                if(!PlayerUtil.IsVisible(screenPos)) continue;
                float scaledDist = screenPos.y - topScreen.y;
                
                Renderer.DrawCenteredBox(new Vector2(screenPos.x + 1, screenPos.y + 1), new Vector2(scaledDist, scaledDist * 1.5f), Color.black, 2);
                Renderer.DrawCenteredBox(screenPos, new Vector2(scaledDist, scaledDist * 1.5f), playerMoveC.nickLabel.color, 2);
            }
            if (aimedPos == Vector3.zero) return;
            Vector3 ap = PlayerUtil.WorldToScreenPoint(main, aimedPos);
            Renderer.DrawCenteredBox(new Vector2(ap.x + 1, ap.y + 1), new Vector2(50, 50), Color.black, 3);
            Renderer.DrawCenteredBox(ap, new Vector2(50, 50), Color.magenta, 2);
        }
    }
}