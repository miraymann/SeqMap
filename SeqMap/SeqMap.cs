using System;
using System.Collections.Generic;
using System.Linq;
using StructureMap;
using StructureMap.Pipeline;

namespace SeqMap
{
    public static class SeqMap
    {
	    public static ISetNameWizardStep<T> ForSequenceOf<T>(this IRegistry registry) =>
			new Wizard<T>(registry);

		private class Wizard<TContract> 
			: ISetNameWizardStep<TContract>
			, IAddOrUseWizardStep<TContract>
			, ISetNextItemWizardStep<TContract>
		{
			private const string DefaultProfile = "Default";

			private readonly IRegistry _registry;
			private readonly IDictionary<string, int> _profileIndexes;
			private readonly IList<ItemMap> _itemsMap;

			private int _lastProfileIndex = 0;
			private string _sequenceName;
			private RegistrationType _registartionType;

			private ItemMap CurrentItem =>
				_itemsMap[_itemsMap.Count - 1];

			public Wizard(IRegistry registry)
			{
				_profileIndexes = new Dictionary<string, int>();
				_itemsMap = new List<ItemMap>();
				_registry = registry;
			}

			public IAddOrUseWizardStep<TContract> Named(string name)
			{
				_sequenceName = name;
				return this;
			}

			public ISetNextItemWizardStep<TContract> AddItems() =>
				Init(RegistrationType.Add);

			public ISetNextItemWizardStep<TContract> UseItems() => 
				Init(RegistrationType.Use);

			private ISetNextItemWizardStep<TContract> Init(
				RegistrationType registrationType)
			{
				_registartionType = registrationType;
				IndexProfile(DefaultProfile);
				return this;
			}

			public ISetNextItemWizardStep<TContract> NextIsNamed(
				string name,
				params string[] profiles) =>

				SetNext(profiles, registry =>
					registry.For<TContract>()
						    .Add(context => context.GetInstance<TContract>(name))
						    .Named(CurrentItem.Name));

			public ISetNextItemWizardStep<TContract> NextIsNamed<TImplementation>(
				string name,
				params string[] profiles)
				where TImplementation : TContract =>

				SetNext(profiles, registry =>
					registry.For<TContract>()
						.Add(context => context.GetInstance<TImplementation>(name))
						.Named(CurrentItem.Name));

			public ISetNextItemWizardStep<TContract> NextIs<TImplementation>(
				params string[] profiles)
				where TImplementation : TContract =>

				SetNext(profiles, registry =>
					registry.For<TContract>()
						    .Add(context => context.GetInstance<TImplementation>())
						    .Named(CurrentItem.Name));

			public ISetNextItemWizardStep<TContract> AddNext<TImplementation>(
				params string[] profiles)
				where TImplementation : TContract =>

				SetNext(profiles, registry =>
					registry.For<TContract>()
						    .Add<TImplementation>()
						    .Named(CurrentItem.Name));

			private ISetNextItemWizardStep<TContract> SetNext(
				string[] profiles,
				Action<IProfileRegistry> registerItemIn)
			{
				var item = new ItemMap();
				_itemsMap.Add(item);

				void MapHandler(string profileName, IProfileRegistry profileRegistry)
				{
					registerItemIn(profileRegistry);
					IndexProfile(profileName);

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
				var defaultProfileMask = 1UL << _profileIndexes[DefaultProfile];
				CreateRegistrator(defaultProfileMask)(_registry);
				
				foreach (var profile in _profileIndexes.Keys)
				{
					if (profile == DefaultProfile)
						continue;

					var profilingMask = (1UL << _profileIndexes[profile]) | defaultProfileMask;
					var registerSequence = CreateRegistrator(profilingMask);
					_registry.Profile(profile, registerSequence);
				}
			}

			private void IndexProfile(string profileName)
			{
				if (!_profileIndexes.ContainsKey(profileName))
					_profileIndexes.Add(profileName, _lastProfileIndex++);
			}

			private Action<IProfileRegistry> CreateRegistrator(ulong profilingMask) => registry =>
			{
				const string description = "SeqMap IEnumerable creation";

				var registartionStep1 = registry.For<IEnumerable<TContract>>();
				LambdaInstance<IEnumerable<TContract>, IEnumerable<TContract>> registartionStep2;

				if (_registartionType == RegistrationType.Add)
				{
					registartionStep2 = registartionStep1
						.Add(description, CreateItemsSelector(profilingMask));
				}
				else // if (_registartionType == RegistrationType.Use)
				{
					registartionStep2 = registartionStep1
						.Use(description, CreateItemsSelector(profilingMask));
				}

				if (_sequenceName != null)
					registartionStep2.Named(_sequenceName);
			};

			private Func<IContext, IEnumerable<TContract>> CreateItemsSelector(ulong profilingMask) => 
				context => _itemsMap.Where(o => (o.Profiling & profilingMask) != 0)
									.Select(o => context.GetInstance<TContract>(o.Name));

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

			private enum RegistrationType { Add, Use }
		}

	    public interface IAddOrUseWizardStep<in TContract>
	    {
		    ISetNextItemWizardStep<TContract> AddItems();
		    ISetNextItemWizardStep<TContract> UseItems();
		}

		public interface ISetNameWizardStep<in TContract>
			: IAddOrUseWizardStep<TContract>
		{
			IAddOrUseWizardStep<TContract> Named(string name);
		}

		public interface ISetNextItemWizardStep<in TContract>
		{
			ISetNextItemWizardStep<TContract> AddNext<TImplementation>(
				params string[] profiles)
				where TImplementation : TContract;

			ISetNextItemWizardStep<TContract> NextIs<TImplementation>(
				params string[] profiles)
				where TImplementation : TContract;

			ISetNextItemWizardStep<TContract> NextIsNamed(
				string name,
				params string[] profiles);

			ISetNextItemWizardStep<TContract> NextIsNamed<TImplementation>(
				string name,
				params string[] profiles)
				where TImplementation : TContract;

			void End();
		}
    }
}
