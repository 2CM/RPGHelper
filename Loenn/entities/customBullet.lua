local mods = require("mods")
local utils = require("utils")
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local drawableRectangle = require("structs.drawable_rectangle")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper", "CommunalHelper")
local drawing = require("utils.drawing")

local curveTypes = {
    ["Linear"] = "Linear",
    ["Quadratic"] = "Quadratic",
    ["Cubic"] = "Cubic",
}

local entity = {}


entity.nodeLimits = {2, -1}
entity.name = "RPGHelper/CustomBullet";
entity.placements = {
    name = "CustomBullet",
    data = {
        spritePath = "danger/starfish13",
        spriteFrames = 0,
        spriteSpeed = 0,
        speed = 120,
        activeFlag = "",
        depth = 0,
        appearDelay = 0,
        moveDelay = 0,
        appearSFX = "",
        moveSFX = "",
        hitboxWidth = 8,
        hitboxHeight = 8,
        fadeOutTime = 1,
        fadeInTime = 1,
        indicatorSFX = "",
        respawnTime = 1,
        repeatDelay = 0.5,
        repeatAmount = 5,
        accelTime = 0,
        decelTime = 0,
        curveType = "Linear",
        rotationSpeed = 0,
        autoRotate = true,
        indicator = false,
        respawn = true,
        anchor = false,
        deleteOnContact = false,
        deleteOnGround = false,
    }
}


entity.fieldInformation = {
    curveType = {
        options = curveTypes,
        editable = false
    }
}

-- i hate lua
function table.shallow_copy(t)
    local t2 = {}
    for k,v in pairs(t) do
        t2[k] = v
    end

    return t2
end

function entity.sprite(room, entity)
    local sprites = {}
    local actualNodes = {entity}
    
    if not (drawableSprite.fromTexture(entity.spritePath, entity) == nil) then
        table.insert(sprites, drawableSprite.fromTexture(entity.spritePath, entity));
    elseif not (drawableSprite.fromTexture(entity.spritePath .. "00", entity) == nil) then
        table.insert(sprites, drawableSprite.fromTexture(entity.spritePath .. "00", entity));
    else
        table.insert(sprites, drawableSprite.fromTexture("decals/generic/algae_c", entity));
    end

    

    for _,v in pairs(entity.nodes) do
        table.insert(actualNodes, v)
    end

    table.remove(actualNodes, 2);

    if #actualNodes >= 2 then
        for i = 1, #actualNodes - 2, 1 do
            table.insert(sprites, drawableLine.fromPoints({actualNodes[i].x, actualNodes[i].y, actualNodes[i + 1].x, actualNodes[i + 1].y}, {1, 1, 1, 0.4}))
        end

        if entity.curveType == "Quadratic" and #actualNodes >= 3 then
            for i = 1, #actualNodes - 3, 2 do
                local a = actualNodes[i + 0]
                local b = actualNodes[i + 1]
                local c = actualNodes[i + 2]

                local points = drawing.getSimpleCurve({a.x, a.y}, {c.x, c.y}, {b.x, b.y}, 24)
                table.insert(sprites, drawableLine.fromPoints(points))
            end
        end if entity.curveType == "Cubic" and #actualNodes >= 4 then
            for i = 1, #actualNodes - 4, 3 do
                local a = actualNodes[i + 0]
                local b = actualNodes[i + 1]
                local c = actualNodes[i + 2]
                local d = actualNodes[i + 3]

                local points = communalHelper.getCubicCurve({a.x, a.y}, {d.x, d.y}, {b.x, b.y}, {c.x, c.y}, 24)
                table.insert(sprites, drawableLine.fromPoints(points))
            end
        end
    end

    -- if drawableSprite.fromTexture(entity.spritePath, entity) == nil then
    --     print("nil")
    -- else
    --     table.insert(sprites, drawableSprite.fromTexture(entity.spritePath, entity));
    -- end


    return sprites
end

function entity.nodeSprite(room, entity, node, index)
    if index == 1 then
        if entity.anchor == true then
            return drawableSprite.fromTexture("playeroutline", node);
        else
            return nil
        end
    end
    -- end if index == 2 then
    --     return drawableLine.fromPoints({entity.x, entity.y, node.x, node.y}, {1, 1, 1, 0.4}):getDrawableSprite();
    -- end if index > 2 then
    --     local line = drawableLine.fromPoints({entity.nodes[index - 1].x, entity.nodes[index - 1].y, node.x, node.y}, {1, 1, 1, 0.4}):getDrawableSprite();

    --     return line;
    -- end
end
  
function entity.nodeRectangle(room, entity, node)
    return utils.rectangle(node.x - 4, node.y - 4, 8, 8)
end

return entity;