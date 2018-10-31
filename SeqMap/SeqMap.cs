using System;
using System.Collections.Generic;
using System.Linq;
using StructureMap;

namespace SeqMap
{
    public static class SeqMap
    {
	    public static IBuilder<T> ForSequenceOf<T>(this IRegistry registry) =>
			new Builder<T>(registry);

		private class Builder<TContract> : IBuilder<TContract>
		{
			private const string DefaultProfile = "Default";

			private readonly IRegistry _registry;
			private readonly IDictionary<string, int> _profileIndexes;
			private readonly IList<ItemMap> _itemsMap;
			private int _lastProfileIndex = 0;

			public Builder(IRegistry registry)
			{
				_profileIndexes = new Dictionary<string, int>();
				_itemsMap = new List<ItemMap>();
				_registry = registry;
			}

			public IBuilder<TContract> Add<TImplementation>(params string[] profiles) 
				where TImplementation : TContract
			{
				var item = new ItemMap();
				_itemsMap.Add(item);

				void MapHandler(string profileName, IProfileRegistry profileRegistry)
				{
					profileRegistry
						.For<TContract>()
						.Add<TImplementation>()
						.Named(item.Name);

					if (!_profileIndexes.ContainsKey(profileName))
						_profileIndexes.Add(profileName, _lastProfileIndex++);
					
					var currentHandler = _itemsMap[_itemsMap.Count - 1];
					currentHandler.Profiling |= 1UL << _profileIndexes[profileName];
				}

				if (!profiles.Any())
					MapHandler(DefaultProfile, _registry);
				else
				{
					foreach (var profileName in profiles)
						_registry.Profile(
							profileName,
							profile => MapHandler(profileName, profile));
				}

				return this;
			}

			public void End()
			{
				var defaultProfileMask =
					1UL << _profileIndexes[DefaultProfile];

				Func<IContext, IEnumerable<TContract>> CreateItemsSelector(ulong profilingMask) => 
					context => _itemsMap.Where(o => (o.Profiling & profilingMask) != 0)
										.Select(o => context.GetInstance<TContract>(o.Name));
				_registry
					.For<IEnumerable<TContract>>()
					.Use("Items sequence calculation", CreateItemsSelector(defaultProfileMask));

				foreach (var profile in _profileIndexes.Keys)
				{
					if (profile == DefaultProfile)
						continue;

					_registry.Profile(profile, 
						registry => registry
							.For<IEnumerable<TContract>>()
							.Use($"Items sequence calculation for {profile}",
								 CreateItemsSelector((1UL << _profileIndexes[profile]) | defaultProfileMask)));
				}
			}

			private class ItemMap
			{
				public ItemMap()
				{
					Name = Guid.NewGuid().ToString();
					Profiling = 0UL;
				}

				public string Name { get; }
				public ulong Profiling { get; set; }
			}
		}

		public interface IBuilder<in TContract>
		{
			IBuilder<TContract> Add<TImplementation>(params string[] profiles) 
				where TImplementation : TContract;

			void End();
		}
    }
}
