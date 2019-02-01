using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StructureMap;
using StructureMap.Pipeline;

namespace SeqMap
{
	public static partial class SeqMap
	{
		public static IAddOrUseWizardStep<T> ForEnumerabletOf<T>(this IRegistry registry) =>
			new Wizard<T>(registry);
		
		private class Wizard<TContract>
			: IAddOrUseWizardStep<TContract>
			, ISetNameOrNextItemWizardStep<TContract>
			, ISetProfilesOrNextItemWitzardStep<TContract>
			, ISetNextItemWizardStep<TContract>
		{
			private const string DefaultProfile = "DEFAULT_PROFILE";

			private delegate void RegisterItem(
				string itemName, 
				IProfileRegistry registry);

			private class State
			{
				public IRegistry Registry { get; set; }
				public IDictionary<string, int> ProfileIndexes { get; set; }
				public IList<ItemMap> ItemsMap { get; set; }
				
				public string SequenceName { get; set; }
				public RegistrationType RegistartionType { get; set; }
				public int LastProfileIndex { get; set; }

				public RegisterItem RegisterItem { get; set; }
				public ProfilesCollection Profiles { get; set; }
			}

			private readonly State _state;

			private Wizard(State state)
			{
				_state = state;
			}

			public Wizard(IRegistry registry)
				: this(new State
				{
					LastProfileIndex = 0,
					ProfileIndexes = new Dictionary<string, int>(),
					ItemsMap = new List<ItemMap>(),
					Registry = registry
				})
			{
				IndexProfile(_state, DefaultProfile);
			}

			public ISetNameOrNextItemWizardStep<TContract> AddSequence()
			{
				_state.RegistartionType = RegistrationType.Add;
				return new Wizard<TContract>(_state);
			}

			public ISetNameOrNextItemWizardStep<TContract> UseSequence()
			{
				_state.RegistartionType = RegistrationType.Use;
				return new Wizard<TContract>(_state);
			}

			public ISetNextItemWizardStep<TContract> Named(string name)
			{
				_state.SequenceName = name;
				return new Wizard<TContract>(_state);
			}

			public ISetProfileOrChooseCtorParamOrSetNextItemWizardStep<TContract> AddNext<TImplementation>()
				where TImplementation : TContract
			{
				SetNextItem(_state);

				return new ChooseCtorParamWizardStep<TImplementation>(
					new ChooseCtorParamWizardStep<TImplementation>.State
					{
						MapCtorParams = instance => instance,
						Base = _state
					});
			}

			public ISetProfilesOrNextItemWitzardStep<TContract> NextIs<TImplementation>() 
				where TImplementation : TContract
			{
				SetNextItem(_state);

				_state.RegisterItem = (itemName, registry) =>
					registry.For<TContract>()
						    .Add(context => context.GetInstance<TImplementation>())
						    .Named(itemName);

				return new Wizard<TContract>(_state);
			}

			public ISetProfilesOrNextItemWitzardStep<TContract> NextIsNamed(string name) =>
				NextIsNamed<TContract>(name);

			public ISetProfilesOrNextItemWitzardStep<TContract> NextIsNamed<TImplementation>(string name) 
				where TImplementation : TContract
			{
				SetNextItem(_state);

				_state.RegisterItem = (itemName, registry) =>
					registry.For<TContract>()
						    .Add(context => context.GetInstance<TImplementation>(name))
						    .Named(itemName);

				return new Wizard<TContract>(_state);
			}

			public ISetNextItemWizardStep<TContract> WhenProfileIs(string profile)
			{
				_state.Profiles = new ProfilesCollection(profile);
				return new Wizard<TContract>(_state);
			}

			public ISetNextItemWizardStep<TContract> WhenProfileIsIn(
				string firtsProfile, 
				string secondProfile, 
				params string[] otherProfiles)
			{
				_state.Profiles = new ProfilesCollection(firtsProfile, secondProfile, otherProfiles);
				return new Wizard<TContract>(_state);
			}

			public void End()
			{
				SetNextItem(_state);

				var defaultProfileMask = 1UL << _state.ProfileIndexes[DefaultProfile];
				CreateRegistrator(defaultProfileMask)(_state.Registry);

				foreach (var profile in _state.ProfileIndexes.Keys)
				{
					if (profile == DefaultProfile)
						continue;

					var profilingMask = (1UL << _state.ProfileIndexes[profile]) | defaultProfileMask;
					var registerSequence = CreateRegistrator(profilingMask);
					_state.Registry.Profile(profile, registerSequence);
				};
			}

			private Action<IProfileRegistry> CreateRegistrator(ulong profilingMask) => registry =>
			{
				const string description = "SeqMap IEnumerable creation";

				var registartionStep1 = registry.For<IEnumerable<TContract>>();
				LambdaInstance<IEnumerable<TContract>, IEnumerable<TContract>> registartionStep2;

				if (_state.RegistartionType == RegistrationType.Add)
				{
					registartionStep2 = registartionStep1
						.Add(description, CreateItemsSelector(profilingMask));
				}
				else // if (_registartionType == RegistrationType.Use)
				{
					registartionStep2 = registartionStep1
						.Use(description, CreateItemsSelector(profilingMask));
				}

				if (_state.SequenceName != null)
					registartionStep2.Named(_state.SequenceName);
			};

			private Func<IContext, IEnumerable<TContract>> CreateItemsSelector(ulong profilingMask) =>
				context => _state.ItemsMap
					.Where(o => (o.Profiling & profilingMask) != 0UL)
					.Select(o => context.GetInstance<TContract>(o.Name));

			private static void IndexProfile(State state, string profileName)
			{
				if (!state.ProfileIndexes.ContainsKey(profileName))
					state.ProfileIndexes.Add(profileName, state.LastProfileIndex ++);
			}

			private static void SetNextItem(State state)
			{
				if (state.RegisterItem == null) return;

				var item = new ItemMap
				{
					Name = Guid.NewGuid().ToString(),
					Profiling = 0UL
				};

				Action<IProfileRegistry> MapItemIn(string profile) => registry =>
				{
					state.RegisterItem(item.Name, registry);
					IndexProfile(state, profile);
					item.Profiling |= 1UL << state.ProfileIndexes[profile];
				};

				if (state.Profiles == null)
					MapItemIn(DefaultProfile)(state.Registry);

				else foreach (var profile in state.Profiles)
					state.Registry.Profile(profile, MapItemIn(profile));

				state.ItemsMap.Add(item);

				state.RegisterItem = null;
				state.Profiles = null;
			}

			private class ChooseCtorParamWizardStep<TImplementation>
				: ISetProfileOrChooseCtorParamOrSetNextItemWizardStep<TContract>
				, IChooseCtorParamOrSetNextItemWizardStep<TContract>
				where TImplementation : TContract
			{
				public delegate SmartInstance<TImplementation, TContract>
					MapCtorParams(SmartInstance<TImplementation, TContract> instance);

				public class State
				{
					public Wizard<TContract>.State Base { get; set; }
					public MapCtorParams MapCtorParams { get; set; }
					public Action Flush { get; set; }
				}

				private readonly State _state;

				public ChooseCtorParamWizardStep(State state)
				{
					_state = state;

					_state.Base.RegisterItem = (itemName, registry) =>
						_state.MapCtorParams(registry.For<TContract>().Add<TImplementation>())
							  .Named(itemName);

					_state.Flush = () => SetNextItem(_state.Base);
				}

				public ISetProfileOrChooseCtorParamOrSetNextItemWizardStep<TContract> AddNext<TNextImplementation>()
					where TNextImplementation : TContract
				{
					_state.Flush();
					return new Wizard<TContract>(_state.Base)
						.AddNext<TNextImplementation>();
				}

				public ISetProfilesOrNextItemWitzardStep<TContract> NextIs<TNextImplementation>()
					where TNextImplementation : TContract
				{
					_state.Flush();
					return new Wizard<TContract>(_state.Base)
						.NextIs<TNextImplementation>();
				}

				public ISetProfilesOrNextItemWitzardStep<TContract> NextIsNamed(string name)
				{
					_state.Flush();
					return new Wizard<TContract>(_state.Base)
						.NextIsNamed(name);
				}

				public ISetProfilesOrNextItemWitzardStep<TContract> NextIsNamed<TNextImplementation>(string name) 
					where TNextImplementation : TContract
				{
					_state.Flush();
					return new Wizard<TContract>(_state.Base)
						.NextIsNamed<TNextImplementation>(name);
				}

				public IChooseCtorParamOrSetNextItemWizardStep<TContract> IntoProfile(string profile)
				{
					_state.Base.Profiles = new ProfilesCollection(profile);
					return new ChooseCtorParamWizardStep<TImplementation>(_state);
				}

				public IChooseCtorParamOrSetNextItemWizardStep<TContract> IntoProfiles(
					string firtsProfile,
					string secondProfile,
					params string[] otherProfiles)
				{
					_state.Base.Profiles = new ProfilesCollection(firtsProfile, secondProfile, otherProfiles);
					return new ChooseCtorParamWizardStep<TImplementation>(_state);
				}

				public void End()
				{
					_state.Flush();
					new Wizard<TContract>(_state.Base)
						.End();
				}

				public ISetCtorArgWizardStep<TContract, TParam> Ctor<TParam>() =>
					new SetCtorArgWizardStep<TParam>(
						new SetCtorArgWizardStep<TParam>.State
						{
							ChooseCtorParam = instance => instance.Ctor<TParam>(),
							Base = _state
						});

				public ISetCtorArgWizardStep<TContract, TParam> Ctor<TParam>(string paramName) =>
					new SetCtorArgWizardStep<TParam>(
						new SetCtorArgWizardStep<TParam>.State
						{
							ChooseCtorParam = instance => instance.Ctor<TParam>(paramName),
							Base = _state
						});


				private class SetCtorArgWizardStep<TParam>
					: ISetCtorArgWizardStep<TContract, TParam>
				{
					public delegate DependencyExpression<SmartInstance<TImplementation, TContract>, TParam>
						ChooseCtorParam(SmartInstance<TImplementation, TContract> instance);

					private delegate SmartInstance<TImplementation, TContract>
						InitCtorParam(DependencyExpression<SmartInstance<TImplementation, TContract>, TParam> ctor);

					public class State
					{
						public ChooseCtorParamWizardStep<TImplementation>.State Base { get; set; }
						public ChooseCtorParam ChooseCtorParam { get; set; }
					}

					private readonly State _state;

					public SetCtorArgWizardStep(State state)
					{
						_state = state;
					}

					public IChooseCtorParamOrSetNextItemWizardStep<TContract> IsNamedInstance(string name)
					{
						SetParam(param => param.IsNamedInstance(name));
						return new ChooseCtorParamWizardStep<TImplementation>(_state.Base);
					}

					public IChooseCtorParamOrSetNextItemWizardStep<TContract> Is(Instance instance)
					{
						SetParam(param => param.Is(instance));
						return new ChooseCtorParamWizardStep<TImplementation>(_state.Base);
					}

					public IChooseCtorParamOrSetNextItemWizardStep<TContract> Is(TParam value)
					{
						SetParam(param => param.Is(value));
						return new ChooseCtorParamWizardStep<TImplementation>(_state.Base);
					}

					public IChooseCtorParamOrSetNextItemWizardStep<TContract> Is<TParamImplementation>() 
						where TParamImplementation : TParam
					{
						SetParam(param => param.Is<TParamImplementation>());
						return new ChooseCtorParamWizardStep<TImplementation>(_state.Base);
					}

					private void SetParam(InitCtorParam mapCtorParam)
					{
						var mapCtorParams = _state.Base.MapCtorParams;
						_state.Base.MapCtorParams = o =>
							mapCtorParam(_state.ChooseCtorParam(mapCtorParams(o)));
					}
				}
			}

			private class ProfilesCollection : IEnumerable<string>
			{
				private readonly string _head, _second;
				private readonly string[] _tail;

				public ProfilesCollection(
					string first = null,
					string second = null,
					params string[] others)
				{
					_head = first;
					_second = second;
					_tail = others;
				}

				public IEnumerator<string> GetEnumerator()
				{
					yield return _head ?? DefaultProfile;
					if (_second != null)
					{
						yield return _second;
						foreach (var profile in _tail)
							yield return profile;
					}
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}
			}
		}

		private class ItemMap
		{
			public string Name { get; set; }
			public ulong Profiling { get; set; }
		}

		private enum RegistrationType { Add, Use }
	}
}
