using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CommunalHelper;
using Celeste.Mod.CommunalHelper.Utils;

namespace Celeste.Mod.RPGHelper;

[CustomEntity("RPGHelper/HealthController")]
[Tracked(false)]
public class HealthController : Entity {
    public Vector2 position;
    public MTexture spriteFull;
    public MTexture spriteDamaged;
    public int health;
    public string flag;
    public string flagOnHit;
    public float iFrames;
    public float space;
    public float scale;
    public bool healBetweenRooms;
    public bool persistent;
    public bool startAtMinHealth;
    public static string fe = "f";

    public int currentHealth;
    public float drawScale = 1;
    public bool fakeLife = false;
    public float iFramesTimer = 0;
    public bool enabled = false;
    public bool initialized = false;
    public bool oldFlag = false;

    public HealthController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        position = new Vector2(data.Float("positionX", 0), data.Float("positionY", 0) * -1);
        spriteFull = data.Attr("spriteFull") == "" ? null : GFX.Game[data.Attr("spriteFull", "collectables/strawberry/normal00")];
        spriteDamaged = data.Attr("spriteDamaged") == "" ? null : GFX.Game[data.Attr("spriteDamaged", "collectables/strawberry/normal08")];
        health = data.Int("health", 3);
        flag = data.Attr("flag", "");
        flagOnHit = data.Attr("flagOnHit", "");
        iFrames = data.Float("iFrames", 0);
        Depth = data.Int("depth", Depths.Player);
        space = data.Float("space", 0);
        scale = data.Float("scale", 1);
        healBetweenRooms = data.Bool("healBetweenRooms", false);
        persistent = data.Bool("persistent", false);
        startAtMinHealth = data.Bool("startAtMinHealth", false);

        if(persistent) this.Tag = Tags.Global;

        currentHealth = startAtMinHealth ? 1 : health;

        Add(new TransitionListener {
            OnOutBegin = delegate {
                if(this.healBetweenRooms) {
                    this.currentHealth = this.health;
                }
            }
        });
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        foreach (HealthController entity in Scene.Tracker.GetEntities<HealthController>()) {
            if (entity.Position == this.Position && entity != this) {
                RemoveSelf();
                
                return;
            }
        }

        // // Console.WriteLine(Scene.Tracker.GetEntities<HealthController>().Count);

        // this.enabled = Scene.Tracker.GetEntities<HealthController>().Count <= 1;
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);

        // foreach (HealthController entity in scene.Tracker.GetEntities<HealthController>()) {
        //     if(entity == this) continue;

        //     if (entity.flag == "" || ((Engine.Scene as Level).Session != null && (Engine.Scene as Level).Session.GetFlag(entity.flag))) {
        //         entity.enabled = true;

        //         return;
        //     }
        // }
    }

    public static void Load() {
        On.Celeste.Player.Die += modPlayerDie;
        On.Celeste.Level.Reload += modLevelReload;
    }

    public static void Unload() {
        On.Celeste.Player.Die -= modPlayerDie;
        On.Celeste.Level.Reload -= modLevelReload;
    }

    public static void modLevelReload(On.Celeste.Level.orig_Reload orig, Level self) {
        foreach (HealthController controller in Engine.Scene.Tracker.GetEntities<HealthController>()) {
            controller.RemoveSelf();
        }
        
        orig(self);
    }

    public static PlayerDeadBody modPlayerDie(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible = false, bool registerDeathInStats = true) {
        if(Engine.Scene.Tracker.GetEntity<HealthController>() != null) {
            HealthController controller = Engine.Scene.Tracker.GetEntities<HealthController>().Where(e => (e as HealthController).enabled == true).First() as HealthController;

            if(controller != null && !evenIfInvincible && controller.enabled) {
                if(controller.iFramesTimer > 0) {
                    return null;
                }

                controller.Add(new Coroutine(controller.loseLifeCoroutine()));
                controller.iFramesTimer = controller.iFrames;

                if(controller.currentHealth > 1) return null;
            }
        } 

        return orig(self, direction, evenIfInvincible, registerDeathInStats);
    }
    
    public static void HealPlayer(int amount) {
        HealthController controller = Engine.Scene.Tracker.GetEntity<HealthController>();

        if(controller != null) {
            controller.currentHealth = Math.Min(controller.currentHealth + amount, controller.health);
        }
    }

    public IEnumerator loseLifeCoroutine() {
        currentHealth--;
        fakeLife = true;
        if(this.flagOnHit != "") (Engine.Scene as Level).Session.SetFlag(this.flagOnHit);
        if(currentHealth > 0) Audio.Play("event:/char/madeline/predeath");

        float timer = 0;

        while(true) {
            timer += Engine.DeltaTime;
            drawScale = 1.4f - timer * 4;

            if(timer > 0.1) fakeLife = false;
            if(timer > 0.18) break;

            yield return null;
        }

        while(timer >= 0.1) {
            timer -= Engine.DeltaTime;
            drawScale = 1.4f - timer * 4;

            yield return null;
        }

        drawScale = 1;
    }

    public override void Update()
    {
        base.Update();

        bool flag = (Engine.Scene as Level).Session.GetFlag(this.flag) || this.flag == "";

        if(!initialized) {
            foreach (HealthController entity in Scene.Tracker.GetEntities<HealthController>()) {
                if (entity.Position == this.Position && entity != this) {
                    RemoveSelf();

                    entity.enabled = true;
                    
                    return;
                }
            }

            this.enabled = flag;

            initialized = true;

            return;
        }

        if(flag && !this.oldFlag) {
            foreach(HealthController c in Engine.Scene.Tracker.GetEntities<HealthController>()) {
                c.enabled = false;
            }

            this.enabled = true;
        }

        if(!flag && this.oldFlag) {
            this.enabled = false;
        }

        oldFlag = flag;

        this.iFramesTimer -= Engine.DeltaTime;
    }

    public override void Render()
    {
        base.Render();

        if(Engine.Scene as Level == null) return;

        if(!this.enabled) return;
        if(spriteDamaged == null || spriteFull == null) return;

        Vector2 basePosition = (Engine.Scene as Level).Camera.Position + new Vector2(0, 164);

        for(int i = 0; i < this.health; i++) {
            float scale = i == this.currentHealth ? drawScale : 1;
            MTexture texture = i < this.currentHealth + (fakeLife ? 1 : 0) ? spriteFull : spriteDamaged;

            texture.DrawCentered(basePosition + this.position + new Vector2(i * (spriteFull.Width + space) * this.scale, 0) + new Vector2(spriteFull.Width, texture.Height) / 2, Color.White, scale * this.scale);
        }
    }
}