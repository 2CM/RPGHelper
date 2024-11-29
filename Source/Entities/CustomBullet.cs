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

public class CustomPath {
    public enum Type {
        Linear,
        Quadratic,
        Cubic
    }

    Type type;
    BakedCurve bakedCurve;
    Dictionary<float, Vector2> bakedPath;

    public float Length;

    public CustomPath(List<Vector2> points, Type type) {
        this.type = type;

        if(points.Count < (int)type + 2) {
            throw new Exception("not enough points in the path");
        }

        if(this.type > 0) {
            this.bakedCurve = new BakedCurve(points.ToArray(), type == Type.Quadratic ? CurveType.Quadratic : CurveType.Cubic, 24);
            this.Length = this.bakedCurve.Length;
        } else {
            float totalDistance = 0;

            this.bakedPath = new Dictionary<float, Vector2>();
            this.bakedPath.Add(0, points[0]);

            for(int i = 1; i < points.Count; i++) {
                totalDistance += Vector2.Distance(points[i - 1], points[i]);
                
                if(points[i - 1] == points[i]) totalDistance += 0.0001f;

                bakedPath.Add(totalDistance, points[i]);
            }


            this.Length = totalDistance;
        }
    }

    public void GetData(float t, out Vector2 point, out Vector2 direction) {
        if(this.type > 0) {
            this.bakedCurve.GetAllByDistance(t, out point, out direction);

            direction = direction.SafeNormalize();

            return;
        } else {
            if(t <= 0) {
                point = this.bakedPath.Values.ElementAt(0);
                direction = (bakedPath.Values.ElementAt(0) - bakedPath.Values.ElementAt(1)).SafeNormalize();

                return;
            }

            if(t >= this.bakedPath.Keys.Last()) {
                point = this.bakedPath.Values.ElementAt(this.bakedPath.Count - 1);
                direction = (bakedPath.Values.ElementAt(this.bakedPath.Count - 2) - bakedPath.Values.ElementAt(this.bakedPath.Count - 1)).SafeNormalize();

                return;
            }

            for(int i = 0; i < bakedPath.Count; i++) {
                if(bakedPath.Keys.ElementAt(i) > t) {
                    float lerp = (t - bakedPath.Keys.ElementAt(i - 1))/(bakedPath.Keys.ElementAt(i) - bakedPath.Keys.ElementAt(i - 1));

                    point = Vector2.Lerp(bakedPath.Values.ElementAt(i - 1), bakedPath.Values.ElementAt(i), lerp);
                    direction = (bakedPath.Values.ElementAt(i - 1) - bakedPath.Values.ElementAt(i)).SafeNormalize();

                    return;
                }
            }
        }

        point = Vector2.Zero;
        direction = Vector2.UnitX;
    }
}

[CustomEntity("RPGHelper/CustomBullet")]
public class CustomBullet : Actor {
    public class BulletEntity {
        public Vector2 position;
        public float rotation;
        public PlayerCollider collider;
        public CustomBullet parent;
        public float opacity = 0f;
        public bool dead = false;

        public BulletEntity(CustomBullet parent) {
            this.parent = parent;
            this.position = parent.Position;
            this.rotation = 0;
            this.collider = new PlayerCollider(parent.collidePlayer, new Hitbox(parent.hitboxWidth, parent.hitboxHeight, parent.hitboxWidth * -0.5f, parent.hitboxHeight * -0.5f));
            // this.collider.Collider.Position += parent.offset;
            parent.Add(collider);
        }

        public void Update(Vector2 position, float rotation, float opacity) {
            this.position = position;
            this.rotation = rotation;
            this.opacity = opacity;
            // this.collider.Collider.Position += this.position - oldPosition;
            this.collider.Collider.Position = this.position - this.parent.Position + new Vector2(parent.hitboxWidth * -0.5f, parent.hitboxHeight * -0.5f);
            
            Player player = Engine.Scene.Tracker.GetEntity<Player>();


            if(player != null && this.collider.Active && this.collider.Check(player)) {
                player.Die((player.Center - this.position).SafeNormalize());
            }

            if(parent.deleteOnContact && player != null && this.collider.Check(player)) {
                this.collider.Active = false;

                parent.Remove(this.collider);

                dead = true;
            }

            if(parent.deleteOnGround && this.collider.Active) {
                foreach(Solid solid in Engine.Scene.Tracker.Entities[typeof(Solid)]) {
                    parent.Collider = collider.Collider;
                    bool res = solid.CollideCheck(parent);
                    parent.Collider = null;

                    if(res) {
                        parent.Remove(this.collider);

                        dead = true;

                        Console.WriteLine("osijef");

                        return;
                    }
                }
            }
        }

        public void Render() {
            if(this.dead) return;

            parent.sprites.ElementAt(
                parent.spriteFrames == 0 ?
                    0 :
                (int)Math.Floor(parent.pathProgress * parent.spriteSpeed) % parent.spriteFrames
            ).DrawCentered(
                this.position,
                new Color(opacity, opacity, opacity, opacity),
                1f,
                this.rotation
                // parent.autoRotate ? this.rotation : MathF.Tau * parent.pathProgress * parent.rotationSpeed
            );
        }
    }

    public List<MTexture> sprites;
    public int spriteFrames = 0;
    public float spriteSpeed = 0;
    public float speed = 60;
    public string activeFlag;
    public float appearDelay;
    public float moveDelay;
    public string appearSFX;
    public string moveSFX;
    public float hitboxWidth;
    public float hitboxHeight;
    public float fadeOutTime;
    public float fadeInTime;
    public MTexture indicatorSprite;
    public string indicatorSFX;
    public float respawnTime;
    public float repeatDelay;
    public int repeatAmount;
    public float accelTime;
    public float decelTime;
    public float rotationSpeed;
    public bool autoRotate;
    public bool indicator;
    public bool respawn;
    public bool anchor;
    public bool deleteOnContact;
    public bool deleteOnGround;

    public Vector2 originalPosition;

    public float opacity = 0;
    public float rotation = 0;
    public bool indicatorEnabled = false;
    public int indicatorFlashCounter = 0;
    public float pathProgress = 0;
    // public float movementTime = 0;
    // public float tripTime = 0;
    public List<Vector2> mainPath;
    public Vector2 anchorPosition;
    public PlayerCollider playerCollider;
    public CustomPath path;
    public List<BulletEntity> bullets = new List<BulletEntity>();
    public Vector2 offset;

    public CustomBullet(EntityData data, Vector2 offset) : base(data.Position + offset) {
        mainPath = new List<Vector2>(data.NodesWithPosition(offset));
        anchorPosition = mainPath[1];
        mainPath.RemoveAt(1);

        try {
            this.path = new CustomPath(mainPath, Enum.Parse<CustomPath.Type>(data.Attr("curveType")));
        } catch(Exception err) {
            Console.WriteLine(err);
            
            try {
                this.path = new CustomPath(mainPath, CustomPath.Type.Linear);
            } catch {
                RemoveSelf();

                return;
            }
        }

        originalPosition = mainPath[0];
        
        sprites = new List<MTexture>();


        speed = data.Float("speed", 60);
        spriteFrames = data.Int("spriteFrames", 0);
        spriteSpeed = data.Float("spriteSpeed", 10);
        activeFlag = data.Attr("activeFlag", "");
        accelTime = data.Float("accelTime", 0.1f);
        decelTime = data.Float("deceltime", 0.1f);
        Depth = data.Int("depth", Depths.Above);
        appearDelay = data.Float("appearDelay", 0);
        moveDelay = data.Float("moveDelay", 0);
        appearSFX = data.Attr("appearSFX", "");
        moveSFX = data.Attr("moveSFX", "");
        hitboxWidth = data.Float("hitboxWidth", 8);
        hitboxHeight = data.Float("hitboxHeight", 8);
        fadeOutTime = data.Float("fadeOutTime", 0);
        fadeInTime = data.Float("fadeInTime", 0);
        indicatorSFX = data.Attr("indicatorSFX", "");
        respawnTime = data.Float("respawnTime", 0);
        repeatDelay = data.Float("repeatDelay", 0);
        repeatAmount = data.Int("repeatAmount", 0);
        rotationSpeed = data.Float("rotationSpeed", 0);
        autoRotate = data.Bool("autoRotate", true);
        indicator = data.Bool("indicator", false);
        respawn = data.Bool("respawn", false);
        anchor = data.Bool("anchor", false);
        deleteOnContact = data.Bool("deleteOnContact", false);
        deleteOnGround = data.Bool("deleteOnGround", false);

        if(spriteFrames == 0) {
            sprites.Add(GFX.Game[data.Attr("spritePath", "")]);
        } else {
            for(int i = 0; i <= spriteFrames; i++) {
                sprites.Add(GFX.Game[data.Attr("spritePath", "") + (i < 10 ? "0" : "") + i]);
            }
        }
        // this.playerCollider = new PlayerCollider(collidePlayer, new Hitbox(this.hitboxWidth, this.hitboxHeight, this.hitboxWidth * -0.5f, this.hitboxHeight * -0.5f));

        for(int i = 0; i < repeatAmount + 1; i++) {
            this.bullets.Add(new BulletEntity(this));
        }

        // Add(new PlayerCollider(collidePlayer, new Hitbox(hitboxWidth, hitboxHeight, hitboxWidth * -0.5f, hitboxHeight * -0.5f)));

        // for(int i = 1; i < this.mainPath.Count; i++) {
        //     this.tripTime += Engine.DeltaTime * Vector2.Distance(this.mainPath[i], this.mainPath[i-1]) / this.speed;
        // }

        Add(new Coroutine(existCoroutine(), true));
    }

    public void Reinitialize() {
        this.opacity = 0;
        this.Position = this.originalPosition;
        this.pathProgress = 0;
    }

    public static float freakyCurve(float t, float speed, float accel, float decel) { //https://www.desmos.com/calculator/mtlwqwgu9p
        if(t < 0f) {
            return 0f;
        } else if(t < accel) {
            return speed / accel * (t * t / 2f);
        } else if(t > decel + (accel - decel) / 2f + 1f / speed) {
            return 1f;
        } else if(t > 1f / speed + (accel - decel) / 2f) {
            return 1f - speed / decel * MathF.Pow(decel - t + 1f/speed + (accel-decel)/2f, 2f) / 2f;
        } else {
            return speed * t - speed * accel / 2f;
        }
    }

    public IEnumerator existCoroutine() {
        while(true) {
            while(this.activeFlag != "" && !(Engine.Scene as Level).Session.GetFlag(this.activeFlag)) {
                yield return null;
            }

            if(appearDelay != 0) yield return this.appearDelay;
            this.indicatorEnabled = true;
            if(this.indicatorSFX != "") Audio.Play(this.indicatorSFX);

            Player player = Engine.Scene.Tracker.GetEntity<Player>();

            if(player != null && anchor) {
                this.offset = player.Position - anchorPosition;
                this.Position += offset;
            }

            // this.Position = this.Position + offset;

            // if(fadeInTime != 0) {
            //     while(this.opacity < 1.0) {
            //         this.opacity = Calc.Approach(this.opacity, 1, Engine.DeltaTime / fadeInTime);

            //         yield return null;
            //     }
            // } else {
            //     this.opacity = 1;
            // }


            if(this.appearSFX != "") Audio.Play(this.appearSFX);

            // if(moveDelay != 0) yield return this.moveDelay;
            // this.indicatorEnabled = false;
            // // Add(this.playerCollider);
            // Audio.Play(this.moveSFX);
            
            while(true) {
                this.pathProgress += Engine.DeltaTime;
                bool breakOut = false;

                if(pathProgress > moveDelay && indicatorEnabled) {
                    this.indicatorEnabled = false;
                    if(this.moveSFX != "") Audio.Play(this.moveSFX);
                }

                for(int i = 0; i < this.bullets.Count; i++) {
                    // float effectiveTime = pathProgress - i * repeatDelay - fadeInTime;
                    // float smoothedProgress = freakyCurve(effectiveTime, this.speed / this.path.Length, this.accelTime, this.decelTime) * this.path.Length;

                    // float endingTime = this.decelTime + (this.accelTime - this.decelTime) / 2f + 1f / (speed / this.path.Length);

                    // float opacity = effectiveTime < 0 ? (fadeInTime + effectiveTime) / fadeInTime : 
                    //     effectiveTime > endingTime ? (fadeOutTime - (effectiveTime - endingTime)) / fadeOutTime : 1f;


                    float effectiveTime = pathProgress - i * repeatDelay - fadeInTime - moveDelay;
                    float endingTime = (this.accelTime * (this.speed / this.path.Length) > 2 ?
                        MathF.Sqrt(2 * this.accelTime / (this.speed / this.path.Length)) :
                        this.decelTime + (this.accelTime - this.decelTime) / 2f + 1f / (speed / this.path.Length)
                    ) - fadeInTime;

                    if(effectiveTime > 0) {
                        this.bullets[i].collider.Active = true;
                    }
                    
                    if(effectiveTime > endingTime) {
                        this.bullets[i].collider.Active = false;
                    }

                    if(this.bullets[i].dead) continue;

                    float smoothedProgress = freakyCurve(effectiveTime + fadeInTime, this.speed / this.path.Length, this.accelTime, this.decelTime) * this.path.Length;


                    float opacity = effectiveTime < 0 ? (fadeInTime + effectiveTime + moveDelay) / fadeInTime : 
                        effectiveTime > endingTime ? (fadeOutTime - (effectiveTime - endingTime)) / fadeOutTime : 1f;

                    if(effectiveTime > endingTime + fadeOutTime && i == this.bullets.Count - 1) breakOut = true;

                    this.path.GetData(smoothedProgress, out Vector2 position, out Vector2 direction);

                    float autoRotation = MathF.Atan2(direction.Y, direction.X) + MathF.PI;
                    float uniformRotation = MathF.Tau * pathProgress * rotationSpeed;
                    float rotation;

                    if (autoRotate && rotationSpeed != 0) {
                        rotation = effectiveTime < 0 ? uniformRotation : autoRotation;
                    } else {
                        rotation = autoRotate ? autoRotation : uniformRotation;
                    }

                    this.bullets[i].Update(position + offset, rotation, opacity);
                }

                if(breakOut) break;

                // if(this.CollideCheck<Solid>() && deleteOnGround) {
                //     RemoveSelf();

                //     yield break;
                // }

                yield return null;
            }

            // Remove(this.playerCollider);

            if(!(this.respawn && this.respawnTime == 0 && this.fadeOutTime == 0)) {
                // if(this.fadeOutTime != 0) {
                //     while(this.opacity > 0.0) {
                //         this.opacity = Calc.Approach(this.opacity, 0, Engine.DeltaTime / fadeOutTime);

                //         yield return null;
                //     }
                // } else {
                //     this.opacity = 0;
                // }

                if(!this.respawn) break;

                yield return this.respawnTime;
            }

            Reinitialize();
        }

        RemoveSelf();

        yield break;
    }

    public void collidePlayer(Player player) {
        // player.Die((player.Center - base.Center).SafeNormalize() * 10);
    }

    public override void Update()
    {
        base.Update();

        indicatorFlashCounter++;
        if(indicatorFlashCounter == 8) indicatorFlashCounter = 0;

        // this.movementTime += Engine.DeltaTime;
    }

    public override void Render()
    {
        base.Render();

        if(indicatorEnabled && indicator && this.indicatorFlashCounter < 4) {
            for(int i = 0; i < this.path.Length; i += 4) {
                this.path.GetData(i, out Vector2 start, out Vector2 direction1);
                this.path.GetData(i + 4, out Vector2 end, out Vector2 direction2);

                Draw.Line(start + offset, end + offset, Color.Red);
            }

            // for(int i = 1; i < this.mainPath.Count; i++) {
            //     Draw.Line(this.mainPath[i - 1], this.mainPath[i], Color.Red);
            // }
        }
        
        for(int i = this.bullets.Count - 1; i >= 0; i--) {
            bullets[i].Render();
        }

        // sprite.DrawCentered(this.Position, new Color(opacity,opacity,opacity,opacity), 1, rotation);
    }
}