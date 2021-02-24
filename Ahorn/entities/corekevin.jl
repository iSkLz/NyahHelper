module NyahHelperCoreKevin

using ..Ahorn, Maple

@mapdef Entity "nyahhelper/corekevin" CoreKevin(x::Integer, y::Integer, defaultToAngry::Bool = false, alwaysAngry::Bool = false)

const placements = Ahorn.PlacementDict(
    "Core Kevin (Both, Nyah Helper)" => Ahorn.EntityPlacement(
        CoreKevin,
        "rectangle"
    ),
    "Core Kevin (Vertical, Nyah Helper)" => Ahorn.EntityPlacement(
        CoreKevin,
        "rectangle",
        Dict{String, Any}(
            "axes" => "vertical"
        )
    ),
    "Core Kevin (Horizontal, Nyah Helper)" => Ahorn.EntityPlacement(
        CoreKevin,
        "rectangle",
        Dict{String, Any}(
            "axes" => "horizontal"
        )
    ),
)

frameImage = Dict{String, String}(
    "none" => "objects/nyahhelper/corekevin/block00",
    "horizontal" => "objects/nyahhelper/corekevin/block01",
    "vertical" => "objects/nyahhelper/corekevin/block02",
    "both" => "objects/nyahhelper/corekevin/block03"
)

smallFace = "objects/nyahhelper/corekevin/idle_face"
giantFace = "objects/nyahhelper/corekevin/giant_block00"

kevinColor = (103, 29, 29) ./ 255

Ahorn.editingOptions(entity::CoreKevin) = Dict{String, Any}(
    "axes" => Maple.kevin_axes
)

Ahorn.minimumSize(entity::CoreKevin) = 24, 24
Ahorn.resizable(entity::CoreKevin) = true, true

Ahorn.selection(entity::CoreKevin) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CoreKevin, room::Maple.Room)
    axes = lowercase(get(entity.data, "axes", "both"))
    chillout = get(entity.data, "chillout", false)

    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    giant = height >= 48 && width >= 48 && chillout
    face = giant ? giantFace : smallFace
    frame = frameImage[lowercase(axes)]
    faceSprite = Ahorn.getSprite(face, "Gameplay")

    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    Ahorn.drawRectangle(ctx, 2, 2, width - 4, height - 4, kevinColor)
    Ahorn.drawImage(ctx, faceSprite, div(width - faceSprite.width, 2), div(height - faceSprite.height, 2))

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, (i - 1) * 8, 0, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, (i - 1) * 8, height - 8, 8, 24, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, 0, (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, width - 8, (i - 1) * 8, 24, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, 0, 0, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, width - 8, 0, 24, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, 0, height - 8, 0, 24, 8, 8)
    Ahorn.drawImage(ctx, frame, width - 8, height - 8, 24, 24, 8, 8)
end

end