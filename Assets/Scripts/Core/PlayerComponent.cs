using PotatoCat.Core;
using PotatoCat.Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComponent : KinematicObject
{
   #region Public Editor Properties

   public GameObject SpawnLocation;
   public GameObject ProjectilePrefab;
   public AudioClip Damage;
   public AudioClip Death;
   public AudioClip Shooting;
   public int MillisecondsBetweenProjectiles = 500;

   public float MaxSpeed = 7;
   public float JumpTakeOffSpeed = 7;

   public JumpState CurrentJumpState = JumpState.Grounded;
   private bool StopJump;
   public Collider2D Collider2d;
   public AudioSource AudioSource;
   public bool ControlEnabled = true;

   #endregion

   #region Internal Properties

   bool mJump;
   Vector2 mMove;
   SpriteRenderer mSpriteRenderer;
   internal Animator mAnimator;

   private DateTime mLastTimeProjectileFired;

   public static float skJumpModifier = 1.5f;
   public static float skJumpDeceleration = 0.5f;

   public Bounds Bounds => Collider2d.bounds;

   #endregion

   private void Awake()
   {
      mSpriteRenderer = GetComponent<SpriteRenderer>();
      mAnimator = GetComponent<Animator>();
      AudioSource = GetComponent<AudioSource>();
      Collider2d = GetComponent<Collider2D>();
   }

   protected override void Update()
   {
      if (ControlEnabled)
      {
         //
         // Handle Motion
         //

         mMove.x = Input.GetAxis("Horizontal");
         if (CurrentJumpState == JumpState.Grounded && Input.GetButtonDown("Jump"))
         {
            CurrentJumpState = JumpState.PrepareToJump;
         }
         else if (Input.GetButtonUp("Jump"))
         {
            StopJump = true;
         }

         //
         // Handle Projectile
         //

         double msSinceLastProjectile = DateTime.Now.Subtract(mLastTimeProjectileFired).TotalMilliseconds;
         if (Input.GetKey(KeyCode.E) && msSinceLastProjectile >= MillisecondsBetweenProjectiles)
         {
            AudioSource.PlayOneShot(Shooting);

            mLastTimeProjectileFired = DateTime.Now;
            var projectile = Instantiate(ProjectilePrefab);
            Vector3 placement = new Vector3(transform.position.x + 0.2f, transform.position.y + 0.1f, transform.position.z);
            projectile.transform.position = placement;

            if (!mSpriteRenderer.flipX)
            {
               ProjectileController projectileController = projectile.GetComponent<ProjectileController>();
               if (projectileController != null)
               { 
                  projectileController.FireDirection = ProjectileController.Direction.Left;
               }
            }
         }
      }
      else
      {
         mAnimator.SetBool("Walking", false);
         mMove.x = 0;
      }

      UpdateJumpState();
      base.Update();
   }

   void UpdateJumpState()
   {
      mJump = false;
      switch (CurrentJumpState)
      {
         case JumpState.PrepareToJump:
            CurrentJumpState = JumpState.Jumping;
            mJump = true;
            StopJump = false;
            break;
         case JumpState.Jumping:
            if (!IsGrounded)
            {
               //Schedule<PlayerJumped>().player = this;
               CurrentJumpState = JumpState.InFlight;
            }
            break;
         case JumpState.InFlight:
            if (IsGrounded)
            {
               //Schedule<PlayerLanded>().player = this;
               CurrentJumpState = JumpState.Landed;
            }
            break;
         case JumpState.Landed:
            CurrentJumpState = JumpState.Grounded;
            break;
      }
   }

   protected override void ComputeVelocity()
   {
      if (mJump && IsGrounded)
      {
         velocity.y = JumpTakeOffSpeed * skJumpModifier;
         mJump = false;
      }
      else if (StopJump)
      {
         StopJump = false;
         if (velocity.y > 0)
         {
            velocity.y = velocity.y * skJumpDeceleration;
         }
      }

      if (mMove.x > 0.01f)
      {
         mSpriteRenderer.flipX = false;
      }
      else if (mMove.x < -0.01f)
      { 
         mSpriteRenderer.flipX = true;
      }

      mAnimator.SetBool("grounded", IsGrounded);
      mAnimator.SetFloat("velocityX", Mathf.Abs(velocity.x) / MaxSpeed);

      targetVelocity = mMove * MaxSpeed;

      if (targetVelocity.x == 0.0f)
      {
         mAnimator.SetBool("Walking", false);
      }
      else
      {
         mAnimator.SetBool("Walking", true);
      }
   }

   public enum JumpState
   {
      Grounded,
      PrepareToJump,
      Jumping,
      InFlight,
      Landed
   }
}
