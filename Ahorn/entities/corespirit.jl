module NyahHelperCoreSpirit

using ..Ahorn, Maple
@mapdef Entity "nyahhelper/corespirit" CoreSpirit(x::Integer, y::Integer, oneUse::Bool=false, syncWithCoreMode::Bool = false, mode::String="Hot", duration::Real=5f)

const modes = String[
    "Hot",
    "Cold"
]

const placements = Ahorn.PlacementDict(
	"Core Spirit (Hot) (Nyah Helper)" => Ahorn.EntityPlacement(
        CoreSpirit,
        "point",
        Dict{String, Any}(
            "mode" => "Hot"
        )
    ),
	"Core Spirit (Cold) (Nyah Helper)" => Ahorn.EntityPlacement(
        CoreSpirit,
        "point",
        Dict{String, Any}(
            "mode" => "Cold"
        )
    )
)

Ahorn.editingOptions(entity::CoreSpirit) = Dict{String, Any}(
    "mode" => modes
)

function Ahorn.selection(entity::CoreSpirit)
    x, y = Ahorn.position(entity)
    type = get(entity.data, "mode", false)

    if type == "Hot"
        return Ahorn.getSpriteRectangle("objects/nyahhelper/corespirit/hot/idle00.png", x, y)
    else
        return Ahorn.getSpriteRectangle("objects/nyahhelper/corespirit/cold/idle00.png", x, y)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CoreSpirit, room::Maple.Room)
    type = get(entity.data, "mode", false)

    if type == "Hot"
        Ahorn.drawSprite(ctx, "objects/nyahhelper/corespirit/hot/idle00.png", 0, 0)
    else
        Ahorn.drawSprite(ctx, "objects/nyahhelper/corespirit/cold/idle00.png", 0, 0)
    end
end

end