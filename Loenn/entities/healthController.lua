local entity = {}


entity.name = "RPGHelper/HealthController";
entity.placements = {
    name = "HealthController",
    data = {
        positionX = 0,
        positionY = 0,
        spriteFull = "collectables/strawberry/normal00",
        spriteDamaged = "collectables/strawberry/normal08",
        health = 3,
        flag = "",
        flagOnHit = "",
        iFrames = 1,
        depth = 1,
        space = 0,
        scale = 1,
        healBetweenRooms = false,
        persistent = false,
        startAtMinHealth = false,
    }
}

return entity;