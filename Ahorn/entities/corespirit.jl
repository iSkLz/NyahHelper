module NyahHelperCoreSpirit

using ..Ahorn, Maple

@mapdef Entity "nyahhelper/corespirit" CoreSpirit(x::Integer, y::Integer, oneUse::Bool=false, syncWithCoreMode::Bool=false, mode::String="Hot", duration::Real=10.0)

const modes = String[
    "hot",
    "cold"
]

const placements = Ahorn.PlacementDict(
	"Core Spirit (Hot) (Nyah Helper)" => Ahorn.EntityPlacement(
        CoreSpirit,
        "point",
        Dict{String, Any}(
            "mode" => "hot"
        )
    ),
	"Core Spirit (Cold) (Nyah Helper)" => Ahorn.EntityPlacement(
        CoreSpirit,
        "point",
        Dict{String, Any}(
            "mode" => "cold"
        )
    )
)

Ahorn.editingOptions(entity::CoreSpirit) = Dict{String, Any}(
    "mode" => modes
)

function Ahorn.selection(entity::CoreSpirit)
    x, y = Ahorn.position(entity)
	# It's the same selection (for both modes)
	return Ahorn.getSpriteRectangle("objects/nyahhelper/corespirit/cold/idle00", x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CoreSpirit, room::Maple.Room)
    coreMode = get(entity.data, "mode", "hot")

    if coreMode == "hot"
        Ahorn.drawSprite(ctx, "objects/nyahhelper/corespirit/hot/idle00", 0, 0)
    else
        Ahorn.drawSprite(ctx, "objects/nyahhelper/corespirit/cold/idle00", 0, 0)
    end
end

end