using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Monocle;
using Celeste;

namespace NyahHelper.Extensions
{
	public static class SceneExt
	{
		public static Entity CollideFirst(this Scene scene, Vector2 from, Vector2 to)
		{
			if (CollideAll(scene, from, to) is var entities && entities.Count > 0) return entities[0];
			return null;
		}

		public static bool CollideCheck<T>(this Scene scene, Vector2 from, Vector2 to,
			params Entity[] excludes)
			where T : Entity
		{
			HashSet<Entity> excludesList = new HashSet<Entity>(excludes);
			List<Entity> list = scene.Tracker.Entities[typeof(T)];
			for (int i = 0; i < list.Count; i++)
			{
				Entity item = list[i];
				if (!excludesList.Contains(item) && item.Collidable && item.CollideLine(from, to))
				{
					return true;
				}
			}
			return false;
		}

		public static bool CollideCheck<T>(this Scene scene, Vector2 from, Vector2 to, out T result,
			params Entity[] excludes)
			where T : Entity
		{
			HashSet<Entity> excludesList = new HashSet<Entity>(excludes);
			List<Entity> list = scene.Tracker.Entities[typeof(T)];
			for (int i = 0; i < list.Count; i++)
			{
				Entity item = list[i];
				if (!excludesList.Contains(item) && item.Collidable && item.CollideLine(from, to))
				{
					result = (T)item;
					return true;
				}
			}
			result = null;
			return false;
		}

		public static List<Entity> CollideAll(this Scene scene, Rectangle rect)
		{
			List<Entity> entities = new List<Entity>();
			foreach (Entity entity in scene.Entities)
			{
				if (entity.Collidable && entity.CollideRect(rect)) entities.Add(entity);
			}
			return entities;
		}

		public static List<Entity> CollideAll(this Scene scene, Vector2 from, Vector2 to)
		{
			List<Entity> entities = new List<Entity>();
			foreach (Entity entity in scene.Entities)
			{
				if (entity.Collidable && entity.CollideLine(from, to)) entities.Add(entity);
			}
			return entities;
		}
	}
}
