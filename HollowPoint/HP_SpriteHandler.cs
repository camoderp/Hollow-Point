﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using GlobalEnums;
using static Modding.Logger;
using ModCommon.Util;
using System.Reflection;

namespace HollowPoint
{
    class HP_Sprites : MonoBehaviour
    {
        public static GameObject gunSpriteGO;
        public static GameObject flashSpriteGO;
        public static GameObject muzzleFlashGO;
        public static GameObject whiteFlashGO;

        System.Random shakeNum = new System.Random();
        static private Vector3 defaultWeaponPos = new Vector3(-0.2f, -0.84f, -0.0001f);

        int rotationNum = 0;

        public static float lowerGunTimer = 0;
        float spriteRecoilHeight = 0;
        float spriteSprintDropdownHeight = 0;

        public static bool isFiring = false;
        public static bool startFiringAnim = false;
        public static bool idleAnim = true;
        public static bool isWallClimbing = false;

        public static GameObject transformSlave = new GameObject("slaveTransform", typeof(Transform));
        public static Transform ts;

        bool isSprinting = false;
        bool? prevFaceRightVal;

        public void Start()
        {
            Log("[HOLLOW POINT] Intializing Weapon Sprites");
            StartCoroutine(SpriteRoutine());
        }

        //Initalizes the sprite game objects
        IEnumerator SpriteRoutine()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);

            prevFaceRightVal = HeroController.instance.cState.facingRight;

            gunSpriteGO = new GameObject("HollowPointGunSprite", typeof(SpriteRenderer), typeof(HP_GunSpriteRenderer), typeof(AudioSource));
            gunSpriteGO.transform.position = HeroController.instance.transform.position;
            gunSpriteGO.transform.localPosition = new Vector3(0,0,0);
            gunSpriteGO.SetActive(true);

            ts = transformSlave.GetComponent<Transform>();
            //ts.transform.SetParent(HeroController.instance.transform);
            gunSpriteGO.transform.SetParent(ts);

            whiteFlashGO = HeroController.instance.GetAttr<GameObject>("dJumpFlashPrefab");

            LoadAssets.spriteDictionary.TryGetValue("muzzleflash.png", out Texture2D muzzleflashTex);
            muzzleFlashGO = new GameObject("bulletFadePrefabObject", typeof(SpriteRenderer));
            muzzleFlashGO.GetComponent<SpriteRenderer>().sprite = Sprite.Create(muzzleflashTex,
                new Rect(0, 0, muzzleflashTex.width, muzzleflashTex.height),
                new Vector2(0.5f, 0.5f), 150);

            DontDestroyOnLoad(whiteFlashGO);
            DontDestroyOnLoad(transformSlave);
            DontDestroyOnLoad(gunSpriteGO);
            DontDestroyOnLoad(muzzleFlashGO);
        }

        public void LateUpdate()
        {
            //if (HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name.Contains("Sprint") && !AmmunitionControl.gunHeatBreak)
            isWallClimbing = HeroController.instance.cState.wallSliding;

            //This just makes it so the gun is more stretched out on wherever the knight is facing, rather than staying in his center
            int directionMultiplier = (HeroController.instance.cState.facingRight) ? 1 : -1;

            //Make it so the gun stretches out more on the opposite if the player is wall sliding
            if (isWallClimbing) directionMultiplier *= -1;

            //fuck your standard naming conventions, if it works, it fucking works
            float howFarTheGunIsAwayFromTheKnightsBody = (HP_WeaponHandler.currentGun.gunName == "Nail") ? 0.20f : 0.35f; //|| HP_HeatHandler.overheat
            float howHighTheGunIsAwayFromTheKnightsBody = (HP_WeaponHandler.currentGun.gunName == "Nail") ? -0.9f : -1.1f; // || HP_HeatHandler.overheat

            ts.transform.position = HeroController.instance.transform.position + new Vector3(howFarTheGunIsAwayFromTheKnightsBody * directionMultiplier, howHighTheGunIsAwayFromTheKnightsBody, -0.001f); ;
            //gunSpriteGO.transform.position = HeroController.instance.transform.position + new Vector3(0.2f * directionMultiplier, -1f, -0.001f);
            

            //:TODO: Tentative changes
            // gunSpriteGO.transform.localPosition = gunSpriteGO.transform.position + new Vector3(0.2f * directionMultiplier, -1f, -0.001f);
            defaultWeaponPos = gunSpriteGO.transform.position + new Vector3(0.2f * directionMultiplier, -1f, -0.001f);

            //flips the sprite on player face direction
            float x = gunSpriteGO.transform.eulerAngles.x;
            float y = gunSpriteGO.transform.eulerAngles.y;
            float z = gunSpriteGO.transform.eulerAngles.z;
            bool faceRight = HeroController.instance.cState.facingRight;

            if (isWallClimbing)
            {
                gunSpriteGO.transform.eulerAngles = (faceRight) ? new Vector3(x, 0, z) : new Vector3(x, 180, z);
            }
            else
            {
                gunSpriteGO.transform.eulerAngles = (faceRight) ? new Vector3(x, 180, z) : new Vector3(x, 0, z);
            }

            //the player starts shooting
            ShootAnim();
          
            //player starts running
            SprintAnim();

            //weapon in the back when the nail is the current active weapon
            WeaponBehindBack();
            /*
            Log("=============================================");
            Log("KNIGHT POSITION " + HeroController.instance.transform.position);
            Log("KNIGHT LOCAL POSITION" + HeroController.instance.transform.localPosition);
            
            Log("TS POSITION " + ts.position);
            Log("TS LOCAL POSITION" + ts.localPosition);
            

            Log("GUN POSITION " +gunSpriteGO.transform.position);
            Log("GUN LOCAL POSITION" +gunSpriteGO.transform.localPosition);
            */


        }

        public void ShootAnim()
        {
            if (startFiringAnim)
            {
                //Log("PLAYER IS NOW FIRING");
                isFiring = true;
                idleAnim = false;
                startFiringAnim = false;
                StartCoroutine(ShootAnimation());
            }
        }

        
        public void SprintAnim()
        {
            if (isFiring) //If the player fires, make it so that they put the gun at a straight angle, otherwise make the gun lower
            {
                StopCoroutine("SprintingShake");
                lowerGunTimer -= Time.deltaTime;
                gunSpriteGO.transform.SetRotationZ(SpriteRotation() * -1); //Point gun at the direction you are shooting

                gunSpriteGO.transform.localPosition = new Vector3(gunSpriteGO.transform.localPosition.x, 0.10f, -0.0001f);

                if (lowerGunTimer < 0)
                {
                    isFiring = false;
                    isSprinting = false;
                }
            }
            else if (HeroController.instance.hero_state == ActorStates.running && !isFiring) //Shake gun a bit while moving
            {
                // gunSpriteGO.transform.SetRotationZ(25); 
                if (!isSprinting) //This bool check prevents the couroutine from running multiple times && !HP_WeaponHandler.currentGun.gunName.Equals("Nail")
                {
                    StartCoroutine("SprintingShake");
                    StartCoroutine("SprintingShakeRotation");
                    isSprinting = true;
                }
            }
            //Idle animation/ Knight standing still
            else if (!isFiring)
            {
                isSprinting = false;
                StopCoroutine("SprintingShake");
                StopCoroutine("SprintingShakeRotation");
                //gunSpriteGO.transform.localPosition = defaultWeaponPos;
                gunSpriteGO.transform.SetRotationZ(24);
                gunSpriteGO.transform.localPosition = new Vector3(gunSpriteGO.transform.localPosition.x, 0, -0.001f);
            }
        }

        void WeaponBehindBack()
        {
            if (HP_WeaponHandler.currentGun.gunName == "Nail") //HP_HeatHandler.overheat
            {
                gunSpriteGO.transform.SetRotationZ(-23); // 23 
                gunSpriteGO.transform.SetPositionZ(0.01f);
                // gunSpriteGO.transform.localPosition = new Vector3(-0.01f, -0.84f, 0.0001f); 

                if (HeroController.instance.hero_state == ActorStates.running)
                {
                    gunSpriteGO.transform.SetRotationZ(-17);
                }
            }
            else
            {
                gunSpriteGO.transform.localPosition = new Vector3(gunSpriteGO.transform.localPosition.x, gunSpriteGO.transform.localPosition.y, -0.0001f);
            }
        }

        //================================ ANIMATION COROUTINES ======================================== 
        IEnumerator SprintingShake()
        {
            spriteSprintDropdownHeight = 0;

            while (true)
            {
                yield return new WaitForSeconds(0.02f);
                float y = Mathf.Sin(Time.time * 16)/100;
                //gunSpriteGO.transform.SetRotationZ(shakeNum.Next(15, 24));
                gunSpriteGO.transform.localPosition += new Vector3(0, y, 0);
            }
        }

        IEnumerator SprintingShakeRotation()
        {
            /*
            while (true)
            {
                yield return new WaitForSeconds(0.09f);
                if (!HeroController.instance.cState.dashing)
                {
                    gunSpriteGO.transform.SetRotationZ(shakeNum.Next(18, 26));
                }
                else
                {
                    gunSpriteGO.transform.SetRotationZ(shakeNum.Next(26, 40));
                }
 
            }
            */

            while (true)
            {
                yield return new WaitForSeconds(0.082f);
                float y = Mathf.Sin(Time.time * 10) * 8;
                y += 24;
                gunSpriteGO.transform.SetRotationZ(y);
                //gunSpriteGO.transform.localPosition += new Vector3(0, y, 0);
            }
        }

        IEnumerator ShootAnimation()
        {
            float face = (HeroController.instance.cState.facingRight) ? 1 : -1;
            gunSpriteGO.transform.localPosition = new Vector3(-0.20f*face, gunSpriteGO.transform.localPosition.y, gunSpriteGO.transform.localPosition.z);
            gunSpriteGO.transform.SetRotationZ(gunSpriteGO.transform.rotation.z + shakeNum.Next(-5,6));
            yield return new WaitForSeconds(0.1f);
            gunSpriteGO.transform.localPosition = new Vector3(0, gunSpriteGO.transform.localPosition.y, gunSpriteGO.transform.localPosition.z);
        }

        //==================================Utilities==================================================

        //returns the degree of the gun's sprite depending on what the player inputs while shooting
        //basically it just rotates the gun based on shooting direction
        static float SpriteRotation()
        {
            if (InputHandler.Instance.inputActions.up.IsPressed && !(InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed))
            {
                return 90;
            }

            if (InputHandler.Instance.inputActions.down.IsPressed && !(InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed))
            {
                return -90;
            }

            if (InputHandler.Instance.inputActions.up.IsPressed)
            {
                if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                {
                    return 45;
                }
            }
            else if (InputHandler.Instance.inputActions.down.IsPressed)
            {
                if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                {
                    return -45;
                }
            }

            return 0;
        }

        public static void StartGunAnims()
        {
            startFiringAnim = true;
            isFiring = false;
            isFiring = true;
            lowerGunTimer = 0.4f;
        }

        public static void StartFlash()
        {
            GameObject flash = Instantiate(whiteFlashGO, HeroController.instance.transform.position + new Vector3(0, 0, -1), new Quaternion(0, 0, 0, 0));
            flash.SetActive(true);
        }

        public static float SpriteRotationWallSlide()
        {
            if (HeroController.instance.cState.wallSliding)
            {
                if (InputHandler.Instance.inputActions.up.IsPressed)
                {
                    if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                    {
                        return 90;
                    }
                }
                else if (InputHandler.Instance.inputActions.down.IsPressed)
                {
                    if (InputHandler.Instance.inputActions.right.IsPressed || InputHandler.Instance.inputActions.left.IsPressed)
                    {
                        return -90;
                    }
                }
            }
            return 0;
        }

        public void OnDestroy()
        {
            Destroy(gunSpriteGO);
        }

    }

    class HP_GunSpriteRenderer : MonoBehaviour
    {
        public static SpriteRenderer gunRenderer;
        public static Dictionary<String, Sprite> weaponSpriteDicitionary = new Dictionary<String, Sprite>();

        private const int PIXELS_PER_UNIT = 180;

        public void Start()
        {
            gunRenderer = gameObject.GetComponent<SpriteRenderer>();

            LoadAssets.spriteDictionary.TryGetValue("Weapon_RifleSprite.png", out Texture2D rifleTextureInit);
            gunRenderer.sprite = Sprite.Create(rifleTextureInit,
                new Rect(0, 0, rifleTextureInit.width, rifleTextureInit.height),
                new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);

            foreach (KeyValuePair<String, Texture2D> wepTexture in LoadAssets.spriteDictionary)
            {
                if (wepTexture.Key.Contains("Weapon"))
                {
                   Texture2D texture = wepTexture.Value;
                   Sprite s = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), PIXELS_PER_UNIT);

                    weaponSpriteDicitionary.Add(wepTexture.Key, s);
                }
            }

            gunRenderer.color = Color.white;
            gunRenderer.enabled = true;
        }

        public static void SwapWeapon(String weaponName)
        {
            if (weaponName.Equals("Nail")) return;
            try
            {
                weaponSpriteDicitionary.TryGetValue(weaponName, out Sprite swapTheCurrentGunSpriteWithThisOne);
                gunRenderer.sprite = swapTheCurrentGunSpriteWithThisOne;
            }
            catch(Exception e)
            {
                Log("No sprite with the name " + weaponName + " was found");
            }
        }

        public void OnDestroy()
        {
            Destroy(gunRenderer);
            Destroy(this);
        }
    }

}