using System;
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
			, ISetNextItemWizardStep<TContract>
		{
			private const string DefaultProfile = "DEFAULT_PROFILE";

			private class State
			{
				public IRegistry Registry { get; set; }
				public IDictionary<string, int> ProfileIndexes { get; set; }
				public IList<ItemMap> ItemsMap { get; set; }
				
				public string SequenceName { get; set; }
				public RegistrationType RegistartionType { get; set; }
				public int LastProfileIndex { get; set; }
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

			public IChooseCtorParamOrSetNextItemWizardStep<TContract> AddNext<TImplementation>(
				params string[] profiles)
				where TImplementation : TContract
			{
				return new ChooseCtorParamWizardStep<TImplementation>(
					new ChooseCtorParamWizardStep<TImplementation>.State
					{
						CurrentItemProfiles = profiles,
						InitParams = o => o,
						Base = _state
					});
			}

			public ISetNextItemWizardStep<TContract> NextIs<TImplementation>(
				params string[] profiles) 
				where TImplementation : TContract
			{
				void Register(string itemName, IProfileRegistry registry) =>
					 registry.For<TContract>()
					 		 .Add(context => context.GetInstance<TImplementation>())
							 .Named(itemName);

				SetNext(_state, profiles, Register);
				return new Wizard<TContract>(_state);
			}

			public ISetNextItemWizardStep<TContract> NextIsNamed(
				string name, params string[] profiles) =>
				NextIsNamed<TContract>(name, profiles);

			public ISetNextItemWizardStep<TContract> NextIsNamed<TImplementation>(
				string name, 
				params string[] profiles) 
				where TImplementation : TContract
			{
				void Register(string itemName, IProfileRegistry registry) =>
					registry.For<TContract>()
						.Add(context => context.GetInstance<TImplementation>(name))
						.Named(itemName);

				SetNext(_state, profiles, Register);
				return new Wizard<TContract>(_state);
			}

			public void End()
			{
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

			private static void SetNext(
				State state,
				string[] profiles, 
				Action<string, IProfileRegistry> registerItem)
			{
				var newItem = new ItemMap
				{
					Name = Guid.NewGuid().ToString(),
					Profiling = 0UL
				};

				void MapHandler(string profileName, IProfileRegistry profileRegistry)
				{
					registerItem(newItem.Name, profileRegistry);
					IndexProfile(state, profileName);
					newItem.Profiling |= 1UL << state.ProfileIndexes[profileName];
				}

				if (!profiles.Any())
					MapHandler(DefaultProfile, state.Registry);

				else foreach (var name in profiles)
						state.Registry.Profile(name, registry => MapHandler(name, registry));

				state.ItemsMap.Add(newItem);
			}

			private class ChooseCtorParamWizardStep<TImplementation>
				: IChooseCtorParamOrSetNextItemWizardStep<TContract>
				where TImplementation : TContract
			{
				public delegate SmartInstance<TImplementation, TContract>
					InitParams(SmartInstance<TImplementation, TContract> instance);

				public class State
				{
					public Wizard<TContract>.State Base { get; set; }
					public InitParams InitParams { get; set; }
					public string[] CurrentItemProfiles { get; set; }
					public Action Flush { get; set; }
				}

				private readonly State _state;

				public ChooseCtorParamWizardStep(State state)
				{
					_state = state;

					void Register(string itemName, IProfileRegistry registry) =>
						_state.InitParams(registry.For<TContract>().Add<TImplementation>())
							  .Named(itemName);
					
					_state.Flush = () =>
						SetNext(_state.Base, _state.CurrentItemProfiles, Register);
				}

				public IChooseCtorParamOrSetNextItemWizardStep<TContract>
					AddNext<TNextImplementation>(params string[] profiles)
					where TNextImplementation : TContract
				{
					_state.Flush();
					return new Wizard<TContract>(_state.Base)
						.AddNext<TNextImplementation>(profiles);
				}

				public ISetNextItemWizardStep<TContract>
					NextIs<TNextImplementation>(params string[] profiles)
					where TNextImplementation : TContract
				{
					_state.Flush();
					return new Wizard<TContract>(_state.Base)
						.NextIs<TNextImplementation>(profiles);
				}

				public ISetNextItemWizardStep<TContract> NextIsNamed(
					string name, 
					params string[] profiles)
				{
					_state.Flush();
					return new Wizard<TContract>(_state.Base)
						.NextIsNamed(name, profiles);
				}

				public ISetNextItemWizardStep<TContract> NextIsNamed<TNextImplementation>(
					string name, 
					params string[] profiles) 
					where TNextImplementation : TContract
				{
					_state.Flush();
					return new Wizard<TContract>(_state.Base)
						.NextIsNamed<TNextImplementation>(name, profiles);
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
							ChooseCtor = instance => instance.Ctor<TParam>(),
							Base = _state
						});

				public ISetCtorArgWizardStep<TContract, TParam> Ctor<TParam>(string paramName) =>
					new SetCtorArgWizardStep<TParam>(
						new SetCtorArgWizardStep<TParam>.State
						{
							ChooseCtor = instance => instance.Ctor<TParam>(paramName),
							Base = _state
						});

				private class SetCtorArgWizardStep<TParam>
					: ISetCtorArgWizardStep<TContract, TParam>
				{
					public delegate DependencyExpression<SmartInstance<TImplementation, TContract>, TParam>
						ChooseCtor(SmartInstance<TImplementation, TContract> instance);

					private delegate SmartInstance<TImplementation, TContract>
						InitOneParam(DependencyExpression<SmartInstance<TImplementation, TContract>, TParam> ctor);

					public class State
					{
						public ChooseCtorParamWizardStep<TImplementation>.State Base { get; set; }
						public ChooseCtor ChooseCtor { get; set; }
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

					private void SetParam(InitOneParam initOneParam)
					{
						var oldInitParams = _state.Base.InitParams;
						_state.Base.InitParams = o =>
							initOneParam(_state.ChooseCtor(oldInitParams(o)));
					}
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
