using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StructureMap;

namespace SeqMap.Tests
{
    public class Tests
    {
	    [Test]
		public void EmptySequence_NoProfile_Test()
	    {
		    var registry = new Registry();
		    registry.ForEnumerabletOf<I>()
			    .UseSequence()
			    .End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    CollectionAssert.IsEmpty(sequence);
		    }
		}

		[Test]
	    public void AddNext_NoProfile_Test()
		{
			var registry = new Registry();
			registry.ForEnumerabletOf<I>()
				.AddSequence()
				.AddNext<A>()
				.AddNext<D>()
				.AddNext<H>()
				.AddNext<G>()
				.AddNext<O>()
				.End();

			using (var container = new Container(registry))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var resultView = SelectViewFrom(sequence);
				Assert.AreEqual("ADHGO", resultView);
			}
		}

		[Test]
	    public void AddNext_ItemsDublication_NoProfile_Test()
	    {
		    var registry = new Registry();
		    registry
			    .ForEnumerabletOf<I>()
			    .AddSequence()
				.AddNext<A>()
			    .AddNext<D>()
			    .AddNext<A>()
			    .AddNext<A>()
			    .AddNext<O>()
				.AddNext<C>()
			    .AddNext<C>()
			    .AddNext<C>()
				.End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var resultView = SelectViewFrom(sequence);
			    Assert.AreEqual("ADAAOCCC", resultView);
		    }
	    }

		[Test]
	    public void AddNext_IsIndependentOfStandardStructureMap_NoProfile_Test()
	    {
		    var registry = new Registry();

		    registry.For<I>().Add<Z>();
		    registry.For<I>().Add<M>();
		    registry.For<I>().Add<L>();

			registry
				.ForEnumerabletOf<I>()
				.AddSequence()
			    .AddNext<A>()
			    .AddNext<D>()
			    .AddNext<H>()
			    .AddNext<G>()
			    .AddNext<O>()
			    .End();

		    registry.For<I>().Add<V>();
		    registry.For<I>().Add<T>();
		    registry.For<I>().Add<R>();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var resultView = SelectViewFrom(sequence);
			    Assert.AreEqual("ADHGO", resultView);
		    }
	    }

	    [Test]
	    public void AddNext_SingleItem_NoProfile_Test()
	    {
		    var registry = new Registry();
		    registry.ForEnumerabletOf<I>()
			    .AddSequence()
			    .AddNext<X>()
			    .End();

		    var container = new Container(registry);
		    var result = container.GetInstance<IEnumerable<I>>()
			    .ToArray();

		    Assert.AreEqual(1, result.Length);
		    Assert.AreEqual("X", result[0].Name);
		}

		[Test]
	    public void AddNext_SingleProfile_Test()
	    {
		    var registry = new Registry();
			registry.ForEnumerabletOf<I>()
				.AddSequence()
			    .AddNext<A>()
			    .AddNext<D>()
			    .AddNext<H>().IntoProfile("Dog")
			    .AddNext<G>()
			    .AddNext<O>().IntoProfile("Dog")
				.AddNext<X>().IntoProfile("Dog")
				.AddNext<Z>()
				.AddNext<E>()
			    .End();

		    string defaultResultView, dogResultView;

			using (var container = new Container(registry))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();

				defaultResultView = SelectViewFrom(sequence);
				dogResultView = SelectViewFrom(dogSequence);
			}

			Assert.Multiple(() =>
			{
				Assert.AreEqual("ADGZE", defaultResultView, "Default Profile");
				Assert.AreEqual("ADHGOXZE", dogResultView, "Dog Profile");
			});
	    }

		[Test]
	    public void AddNext_MultiProfiles_Test()
	    {
		    var registry = new Registry();
		    registry
			    .ForEnumerabletOf<I>()
				.AddSequence()
			    .AddNext<A>()
			    .AddNext<B>().IntoProfiles("1", "22", "333", "4444", "55555", "666666", "7777777")
			    .AddNext<C>().IntoProfiles("1", "22", "333", "4444", "55555", "666666")
			    .AddNext<G>().IntoProfiles("1", "22", "333", "4444", "55555")
			    .AddNext<O>().IntoProfiles("1", "22", "333", "4444")
			    .AddNext<X>().IntoProfiles("1", "22", "333")
			    .AddNext<Z>().IntoProfiles("1", "22")
			    .AddNext<E>().IntoProfile("1")
			    .End();

			var profilesSequencesViews = new Dictionary<string, string>();
		    var expectedProfilesSequencesViews = new Dictionary<string, string>
			    {
				    {"1", "ABCGOXZE"},
				    {"22", "ABCGOXZ"},
				    {"333", "ABCGOX"},
				    {"4444", "ABCGO"},
				    {"55555", "ABCG"},
				    {"666666", "ABC"},
				    {"7777777", "AB"}
			    };

		    string defaultSequenceView;
			using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    defaultSequenceView = SelectViewFrom(sequence);

			    foreach (var profile in expectedProfilesSequencesViews.Keys)
				    using (var profileContainer = container.GetNestedContainer(profile))
				    {
					    var profileSequence = profileContainer.GetInstance<IEnumerable<I>>();
					    profilesSequencesViews.Add(profile, SelectViewFrom(profileSequence));
					}
		    }

		    Assert.Multiple(() =>
		    {
			    Assert.AreEqual("A", defaultSequenceView, "Default Profile");
			    foreach (var profile in expectedProfilesSequencesViews.Keys)
			    {
					Assert.AreEqual(
						expectedProfilesSequencesViews[profile],
						profilesSequencesViews[profile],
						$"{profile} Profile");
			    }
		    });
		}

	    [Test]
	    public void AddNext_EmptySequenceForDefaultProfile_SingleProfile_Test()
	    {
		    var registry = new Registry();
		    registry
			    .ForEnumerabletOf<I>()
			    .UseSequence()
			    .AddNext<A>().IntoProfile("Dog")
			    .AddNext<B>().IntoProfile("Dog")
			    .AddNext<C>().IntoProfile("Dog")
				.End();

			using (var container = new Container(registry))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();

				var defaultSequenceView = SelectViewFrom(sequence);
				var dogSequenceView = SelectViewFrom(dogSequence);

				Assert.Multiple(() =>
				{
					Assert.IsEmpty(defaultSequenceView, "Default Profile");
					Assert.AreEqual("ABC", dogSequenceView, "Dog Profile");
				});
			}
	    }

	    public void AddNext_EmptySequenceForDefaultProfile_TwoProfiles_Test()
	    {
		    var registry = new Registry();
		    registry
			    .ForEnumerabletOf<I>()
			    .UseSequence()
			    .AddNext<A>().IntoProfile("Dog")
			    .AddNext<B>().IntoProfile("Dog")
				.AddNext<Y>().IntoProfiles("Dog", "Cat")
			    .AddNext<L>().IntoProfiles("Cat", "Dog")
			    .AddNext<M>().IntoProfile("Cat")
			    .AddNext<R>().IntoProfile("Cat")
			    .End();
			
			using (var container = new Container(registry))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			using (var catContainer = container.GetNestedContainer("Cat"))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();
				var catSequence = catContainer.GetInstance<IEnumerable<I>>();

				var defaultSequenceView = SelectViewFrom(sequence);
				var dogSequenceView = SelectViewFrom(dogSequence);
				var catSequenceView = SelectViewFrom(catSequence);

				Assert.Multiple(() =>
				{
					Assert.IsEmpty(defaultSequenceView, "Default Profile");
					Assert.AreEqual("ABYL", dogSequenceView, "Dog Profile");
					Assert.AreEqual("YLMR", catSequenceView, "Cat Profile");
				});
			}
		}

		[Test]
	    public void AddNext_UseTwoNamedSequences_NoProfile_Test()
		{
		    var registry = new Registry();
			registry
				.ForEnumerabletOf<I>()				
				.UseSequence()
				.Named("Red")
				.AddNext<A>()
				.AddNext<B>()
				.AddNext<C>()
				.AddNext<D>()
				.AddNext<E>()
				.End();

		    registry
			    .ForEnumerabletOf<I>()
			    .UseSequence()
				.Named("Blue")
			    .AddNext<F>()
			    .AddNext<G>()
			    .AddNext<K>()
			    .AddNext<A>()
			    .AddNext<B>()
			    .End();
			
			using (var container = new Container(registry))
		    {
			    var redSequence = container.GetInstance<IEnumerable<I>>("Red");
			    var blueSequence = container.GetInstance<IEnumerable<I>>("Blue");
			    var defaultSequence = container.GetInstance<IEnumerable<I>>();

			    var redSequenceView = SelectViewFrom(redSequence);
			    var blueSequenceView = SelectViewFrom(blueSequence);
			    var defaultSequenceView = SelectViewFrom(defaultSequence);

			    Assert.Multiple(() =>
			    {
				    Assert.AreEqual("ABCDE", redSequenceView, "Red Sequence");
				    Assert.AreEqual("FGKAB", blueSequenceView, "Blue Sequence");
				    Assert.AreEqual("FGKAB", defaultSequenceView, "Default Sequence");
			    });
			}
		}

	    [Test]
	    public void AddNext_TwoNamedSequences_UseFirstAddSecond_NoProfile_Test()
		{
		    var registry = new Registry();
		    registry
			    .ForEnumerabletOf<I>()
				.UseSequence()
			    .Named("Red")
			    .AddNext<A>()
			    .AddNext<B>()
			    .AddNext<C>()
			    .AddNext<D>()
			    .AddNext<E>()
			    .End();

		    registry
			    .ForEnumerabletOf<I>()
				.AddSequence()
			    .Named("Blue")
			    .AddNext<F>()
			    .AddNext<G>()
			    .AddNext<K>()
			    .AddNext<A>()
			    .AddNext<B>()
			    .End();
			
			using (var container = new Container(registry))
		    {
			    var redSequence = container.GetInstance<IEnumerable<I>>("Red");
			    var blueSequence = container.GetInstance<IEnumerable<I>>("Blue");
			    var defaultSequence = container.GetInstance<IEnumerable<I>>();

			    var redSequenceView = SelectViewFrom(redSequence);
			    var blueSequenceView = SelectViewFrom(blueSequence);
			    var defaultSequenceView = SelectViewFrom(defaultSequence);

			    Assert.Multiple(() =>
			    {
				    Assert.AreEqual("ABCDE", redSequenceView, "Red Sequence");
				    Assert.AreEqual("FGKAB", blueSequenceView, "Blue Sequence");
				    Assert.AreEqual("ABCDE", defaultSequenceView, "Default Sequence");
			    });
			}
		}

	    [Test]
	    public void AddNext_TwoNamedSequences_TwoProfiles_NotIntersectedItems_Test()
	    {
		    var registry = new Registry();
		    registry
			    .ForEnumerabletOf<I>()
				.AddSequence()
			    .Named("Red")
			    .AddNext<A>().IntoProfile("Dog")
			    .AddNext<B>().IntoProfile("Cat")
			    .AddNext<C>().IntoProfile("Dog")
			    .AddNext<X>().IntoProfile("Dog")
			    .AddNext<Y>().IntoProfile("Cat")
			    .End();

		    registry
			    .ForEnumerabletOf<I>()
				.AddSequence()
			    .Named("Blue")
			    .AddNext<A>().IntoProfile("Cat")
			    .AddNext<B>().IntoProfile("Dog")
			    .AddNext<C>().IntoProfile("Cat")
			    .AddNext<X>().IntoProfile("Cat")
			    .AddNext<Y>().IntoProfile("Dog")
			    .End();

		    using (var container = new Container(registry))
			using (var catContainer = container.GetNestedContainer("Cat"))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			{
				var redSequence = container.GetInstance<IEnumerable<I>>("Red");
				var blueSequence = container.GetInstance<IEnumerable<I>>("Blue");
				var redCatSequence = catContainer.GetInstance<IEnumerable<I>>("Red");
				var blueCatSequence = catContainer.GetInstance<IEnumerable<I>>("Blue");
				var redDogSequence = dogContainer.GetInstance<IEnumerable<I>>("Red");
				var blueDogSequence = dogContainer.GetInstance<IEnumerable<I>>("Blue");
				
				var redCatResultView = SelectViewFrom(redCatSequence);
				var blueCatResultView = SelectViewFrom(blueCatSequence);
				var redDogResultView = SelectViewFrom(redDogSequence);
				var blueDogResultView = SelectViewFrom(blueDogSequence);
				
				Assert.Multiple(() =>
				{
					Assert.IsEmpty(redSequence, "Red Sequence, Default Profile");
					Assert.IsEmpty(blueSequence, "Blue Sequence, Default Profile");
					Assert.AreEqual("BY", redCatResultView, "Red Sequence, Cat Profile");
					Assert.AreEqual("ACX", blueCatResultView, "Blue Sequence, Cat Profile");
					Assert.AreEqual("ACX", redDogResultView, "Red Sequence, Dog Profile");
					Assert.AreEqual("BY", blueDogResultView, "Blue Sequence, Cat Profile");
				});
			}
	    }

		[Test]
	    public void AddNext_ParamsAreNamed_NoProfileTest()
	    {
		    var registry = new Registry();
		    registry.For<I>().Add<Q>().Named("QQQ");
		    registry.For<I>().Add<N>().Named("NNN");
		    registry.For<I>().Add<P>().Named("PPP");
		    registry.For<I>().Add<S>().Named("SSS");

			registry.ForEnumerabletOf<I>()
				.UseSequence()
				.AddNext<Triplett>()
					.Ctor<I>("first").IsNamedInstance("QQQ")
					.Ctor<I>("second").IsNamedInstance("NNN")
					.Ctor<I>("last").IsNamedInstance("PPP")
				.AddNext<Triplett>()
					.Ctor<I>("first").IsNamedInstance("SSS")
					.Ctor<I>("second").IsNamedInstance("NNN")
					.Ctor<I>("last").IsNamedInstance("QQQ")
				.AddNext<Triplett>()
					.Ctor<I>("first").IsNamedInstance("QQQ")
					.Ctor<I>("second").IsNamedInstance("PPP")
					.Ctor<I>("last").IsNamedInstance("QQQ")
				.AddNext<Triplett>()
					.Ctor<I>("first").IsNamedInstance("NNN")
					.Ctor<I>("second").IsNamedInstance("SSS")
					.Ctor<I>("last").IsNamedInstance("PPP")
				.End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var sequenceView = SelectViewFrom(sequence);

				Assert.AreEqual("(Q|N|P)(S|N|Q)(Q|P|Q)(N|S|P)", sequenceView);
		    }
		}

	    [Test]
	    public void AddNext_ParamsAreTyped_NoProfileTest()
	    {
		    var registry = new Registry();
		    registry.For<I>().Add<Q>().Named("QQQ");
		    registry.For<I>().Add<N>().Named("NNN");
		    registry.For<I>().Add<P>().Named("PPP");
		    registry.For<I>().Add<S>().Named("SSS");

		    registry.ForEnumerabletOf<I>()
			    .UseSequence()
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is<Q>()
					.Ctor<I>("second").Is<N>()
					.Ctor<I>("last").Is<P>()
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is<S>()
					.Ctor<I>("second").Is<N>()
					.Ctor<I>("last").Is<Q>()
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is<Q>()
					.Ctor<I>("second").Is<P>()
					.Ctor<I>("last").Is<Q>()
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is<N>()
					.Ctor<I>("second").Is<S>()
					.Ctor<I>("last").Is<P>()
			    .End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var sequenceView = SelectViewFrom(sequence);

			    Assert.AreEqual("(Q|N|P)(S|N|Q)(Q|P|Q)(N|S|P)", sequenceView);
			}
	    }

	    [Test]
	    public void AddNext_ParamsAreInstances_NoProfileTest()
	    {
		    var registry = new Registry();
		    var q = registry.For<I>().Add<Q>().Named("QQQ");
		    var n = registry.For<I>().Add<N>().Named("NNN");
		    var p = registry.For<I>().Add<P>().Named("PPP");
		    var s = registry.For<I>().Add<S>().Named("SSS");

		    registry.ForEnumerabletOf<I>()
			    .UseSequence()
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is(q)
					.Ctor<I>("second").Is(n)
					.Ctor<I>("last").Is(p)
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is(s)
					.Ctor<I>("second").Is(n)
					.Ctor<I>("last").Is(q)
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is(q)
					.Ctor<I>("second").Is(p)
					.Ctor<I>("last").Is(q)
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is(n)
					.Ctor<I>("second").Is(s)
					.Ctor<I>("last").Is(p)
			    .End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var sequenceView = SelectViewFrom(sequence);

			    Assert.AreEqual("(Q|N|P)(S|N|Q)(Q|P|Q)(N|S|P)", sequenceView);
			}
	    }

	    [Test]
	    public void AddNext_MixedParamsSetting_NoProfileTest()
	    {
		    var registry = new Registry();
		    var q = registry.For<I>().Add<Q>().Named("QQQ");
		    var n = registry.For<I>().Add<N>().Named("NNN");
		    var p = registry.For<I>().Add<P>().Named("PPP");
		    var s = registry.For<I>().Add<S>().Named("SSS");

		    var snp = registry
			    .For<I>().Add<Triplett>()
					.Ctor<I>("first").Is<S>()
					.Ctor<I>("second").IsNamedInstance("NNN")
					.Ctor<I>("last").Is(p);

			registry.ForEnumerabletOf<I>()
			    .UseSequence()
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is<Q>()
					.Ctor<I>("second").Is<N>()
					.Ctor<I>("last").Is<P>()
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is(s)
					.Ctor<I>("second").Is(n)
					.Ctor<I>("last").Is(q)
			    .AddNext<Triplett>()
					.Ctor<I>("first").IsNamedInstance("QQQ")
					.Ctor<I>("second").IsNamedInstance("PPP")
					.Ctor<I>("last").IsNamedInstance("QQQ")
			    .AddNext<Triplett>()
					.Ctor<I>("first").Is<N>()
					.Ctor<I>("second").IsNamedInstance("SSS")
					.Ctor<I>("last").Is(snp)
			    .End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var sequenceView = SelectViewFrom(sequence);

			    Assert.AreEqual("(Q|N|P)(S|N|Q)(Q|P|Q)(N|S|(S|N|P))", sequenceView);
			}
	    }

		[Test]
	    public void NextIsNamed_NoProfile_Test()
	    {
		    var registry = new Registry();
		    registry.For<I>().Add<Q>().Named("QQQ");
		    registry.For<I>().Add<N>().Named("NNN");
		    registry.For<I>().Add<P>().Named("PPP");
		    registry.For<I>().Add<S>().Named("SSS");

			registry.ForEnumerabletOf<I>()
				.UseSequence()
				.NextIsNamed("NNN")
				.NextIsNamed("SSS")
				.NextIsNamed("PPP")
				.NextIsNamed("SSS")
				.NextIsNamed("SSS")
				.NextIsNamed("QQQ")
				.End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var sequenceView = SelectViewFrom(sequence);

			    Assert.AreEqual("NSPSSQ", sequenceView);
			}
		}

	    [Test]
	    public void NextIsNamed_SingleProfile_Test()
	    {
		    var registry = new Registry();
		    registry.For<I>().Add<Q>().Named("QQQ");
		    registry.For<I>().Add<N>().Named("NNN");
		    registry.For<I>().Add<P>().Named("PPP");
		    registry.For<I>().Add<S>().Named("SSS");

		    registry.ForEnumerabletOf<I>()
			    .UseSequence()
			    .NextIsNamed("NNN")
			    .NextIsNamed("SSS").WhenProfileIs("Dog")
			    .NextIsNamed("PPP")
			    .NextIsNamed("SSS")
			    .NextIsNamed("SSS")
			    .NextIsNamed("QQQ").WhenProfileIs("Dog")
				.End();

			using (var container = new Container(registry))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();

				var sequenceView = SelectViewFrom(sequence);
				var dogSequenceView = SelectViewFrom(dogSequence);

				Assert.Multiple(() =>
				{
					Assert.AreEqual("NPSS", sequenceView, "Default Profile");
					Assert.AreEqual("NSPSSQ", dogSequenceView, "Dog Profile");
				});
			}
	    }

	    [Test]
	    public void NextIsNamed_TwoProfiles_Test()
	    {
		    var registry = new Registry();
		    registry.For<I>().Add<Q>().Named("QQQ");
		    registry.For<I>().Add<N>().Named("NNN");
		    registry.For<I>().Add<P>().Named("PPP");
		    registry.For<I>().Add<S>().Named("SSS");
		    registry.For<I>().Add<X>().Named("XXX");
		    registry.For<I>().Add<M>().Named("MMM");
		    registry.For<I>().Add<T>().Named("TTT");

			registry.ForEnumerabletOf<I>()
			    .UseSequence()
			    .NextIsNamed("NNN")
			    .NextIsNamed("SSS").WhenProfileIs("Dog")
			    .NextIsNamed("PPP").WhenProfileIsIn("Dog", "Cat")
			    .NextIsNamed("SSS").WhenProfileIsIn("Dog", "Cat")
			    .NextIsNamed("SSS")
			    .NextIsNamed("QQQ").WhenProfileIs("Dog")
				.NextIsNamed("PPP")
				.NextIsNamed("MMM")
				.NextIsNamed("SSS").WhenProfileIsIn("Dog", "Cat")
				.NextIsNamed("TTT").WhenProfileIsIn("Dog", "Cat")
				.NextIsNamed("XXX").WhenProfileIs("Dog")
				.NextIsNamed("SSS").WhenProfileIs("Cat")
				.NextIsNamed("PPP")
				.NextIsNamed("TTT")
				.NextIsNamed("TTT").WhenProfileIs("Cat")
				.NextIsNamed("TTT")
				.NextIsNamed("SSS").WhenProfileIs("Dog")
				.NextIsNamed("XXX")
				.NextIsNamed("PPP")
				.NextIsNamed("SSS").WhenProfileIs("Dog")
				.NextIsNamed("MMM").WhenProfileIsIn("Dog", "Cat")
				.End();

			using (var container = new Container(registry))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			using (var catContainer = container.GetNestedContainer("Cat"))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();
				var catSequence = catContainer.GetInstance<IEnumerable<I>>();

				var sequenceView = SelectViewFrom(sequence);
				var dogSequenceView = SelectViewFrom(dogSequence);
				var catSequenceView = SelectViewFrom(catSequence);

				Assert.Multiple(() =>
				{
					Assert.AreEqual("NSPMPTTXP", sequenceView, "Default Profile");
					Assert.AreEqual("NSPSSQPMSTXPTTSXPSM", dogSequenceView, "Dog Profile");
					Assert.AreEqual("NPSSPMSTSPTTTXPM", catSequenceView, "Cat Profile");
				});
			}
	    }

		[Test]
		public void NextIs_NoProfile_Test()
		{
			var registry = new Registry();
			registry.For<I>().Add<A>();
			registry.For<I>().Add<H>();
			registry.For<I>().Add<D>();
			registry.For<I>().Add<O>();
			registry.For<I>().Add<G>();

			registry.ForEnumerabletOf<I>()
				.AddSequence()
				.NextIs<A>()
				.NextIs<D>()
				.NextIs<H>()
				.NextIs<G>()
				.NextIs<O>()
				.End();

			using (var container = new Container(registry))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var resultView = SelectViewFrom(sequence);
				Assert.AreEqual("ADHGO", resultView);
			}
		}

		[Test]
		public void IsNext_ItemsDublication_NoProfile_Test()
		{
			var registry = new Registry();
			registry.For<I>().Add<C>();
			registry.For<I>().Add<O>();
			registry.For<I>().Add<D>();
			registry.For<I>().Add<A>();

			registry
				.ForEnumerabletOf<I>()
				.AddSequence()
				.NextIs<A>()
				.NextIs<D>()
				.NextIs<A>()
				.NextIs<A>()
				.NextIs<O>()
				.NextIs<C>()
				.NextIs<C>()
				.NextIs<C>()
				.End();

			using (var container = new Container(registry))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var resultView = SelectViewFrom(sequence);
				Assert.AreEqual("ADAAOCCC", resultView);
			}
		}
		
		[Test]
		public void NextIs_SingleItem_NoProfile_Test()
		{
			var registry = new Registry();
			registry.For<I>().Add<Z>();
			registry.For<I>().Add<X>();
			registry.For<I>().Add<Y>();

			registry.ForEnumerabletOf<I>()
				.AddSequence()
				.NextIs<X>()
				.End();

			var container = new Container(registry);
			var result = container.GetInstance<IEnumerable<I>>()
				.ToArray();

			Assert.AreEqual(1, result.Length);
			Assert.AreEqual("X", result[0].Name);
		}

		[Test]
		public void NextIs_SingleProfile_Test()
		{
			var registry = new Registry();

			registry.For<I>().Add<A>();
			registry.For<I>().Add<D>();
			registry.For<I>().Add<G>();
			registry.For<I>().Add<Z>();
			registry.For<I>().Add<E>();
			registry.For<I>().Add<H>();
			registry.For<I>().Add<O>();
			registry.For<I>().Add<X>();

			registry.ForEnumerabletOf<I>()
				.AddSequence()
				.NextIs<A>()
				.NextIs<D>()
				.NextIs<H>().WhenProfileIs("Dog")
				.NextIs<G>()
				.NextIs<O>().WhenProfileIs("Dog")
				.NextIs<X>().WhenProfileIs("Dog")
				.NextIs<Z>()
				.NextIs<E>()
				.End();

			string defaultResultView, dogResultView;

			using (var container = new Container(registry))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();

				defaultResultView = SelectViewFrom(sequence);
				dogResultView = SelectViewFrom(dogSequence);
			}

			Assert.Multiple(() =>
			{
				Assert.AreEqual("ADGZE", defaultResultView, "Default Profile");
				Assert.AreEqual("ADHGOXZE", dogResultView, "Dog Profile");
			});
		}

	    [Test]
	    public void NextIs_SingleProfile_TargetItemsAreInTargetProfileOnly_Test()
	    {
		    var registry = new Registry();

		    registry.For<I>().Add<A>();
		    registry.For<I>().Add<D>();
		    registry.For<I>().Add<G>();
		    registry.For<I>().Add<Z>();
		    registry.For<I>().Add<E>();

		    registry.Profile("Dog", profile =>
		    {
			    profile.For<I>().Add<H>();
			    profile.For<I>().Add<O>();
			    profile.For<I>().Add<X>();
		    });

		    registry.ForEnumerabletOf<I>()
			    .AddSequence()
			    .NextIs<A>()
			    .NextIs<D>()
			    .NextIs<H>().WhenProfileIs("Dog")
			    .NextIs<G>()
			    .NextIs<O>().WhenProfileIs("Dog")
			    .NextIs<X>().WhenProfileIs("Dog")
			    .NextIs<Z>()
			    .NextIs<E>()
			    .End();

		    string defaultResultView, dogResultView;

		    using (var container = new Container(registry))
		    using (var dogContainer = container.GetNestedContainer("Dog"))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();

			    defaultResultView = SelectViewFrom(sequence);
			    dogResultView = SelectViewFrom(dogSequence);
		    }

		    Assert.Multiple(() =>
		    {
			    Assert.AreEqual("ADGZE", defaultResultView, "Default Profile");
			    Assert.AreEqual("ADHGOXZE", dogResultView, "Dog Profile");
		    });
	    }

		[Test]
		public void NextIs_MultiProfiles_Test()
		{
			var registry = new Registry();
			
			registry.For<I>().Add<A>();
			registry.For<I>().Add<B>();
			registry.For<I>().Add<O>();
			registry.For<I>().Add<X>();
			registry.For<I>().Add<Z>();
			registry.For<I>().Add<C>();
			registry.For<I>().Add<G>();
			registry.For<I>().Add<E>();

			registry
				.ForEnumerabletOf<I>()
				.AddSequence()
				.NextIs<A>()
				.NextIs<B>().WhenProfileIsIn("1", "22", "333", "4444", "55555", "666666", "7777777")
				.NextIs<C>().WhenProfileIsIn("1", "22", "333", "4444", "55555", "666666")
				.NextIs<G>().WhenProfileIsIn("1", "22", "333", "4444", "55555")
				.NextIs<O>().WhenProfileIsIn("1", "22", "333", "4444")
				.NextIs<X>().WhenProfileIsIn("1", "22", "333")
				.NextIs<Z>().WhenProfileIsIn("1", "22")
				.NextIs<E>().WhenProfileIs("1")
				.End();

			var profilesSequencesViews = new Dictionary<string, string>();
			var expectedProfilesSequencesViews = new Dictionary<string, string>
				{
					{"1", "ABCGOXZE"},
					{"22", "ABCGOXZ"},
					{"333", "ABCGOX"},
					{"4444", "ABCGO"},
					{"55555", "ABCG"},
					{"666666", "ABC"},
					{"7777777", "AB"}
				};

			string defaultSequenceView;
			using (var container = new Container(registry))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				defaultSequenceView = SelectViewFrom(sequence);

				foreach (var profile in expectedProfilesSequencesViews.Keys)
					using (var profileContainer = container.GetNestedContainer(profile))
					{
						var profileSequence = profileContainer.GetInstance<IEnumerable<I>>();
						profilesSequencesViews.Add(profile, SelectViewFrom(profileSequence));
					}
			}

			Assert.Multiple(() =>
			{
				Assert.AreEqual("A", defaultSequenceView, "Default Profile");
				foreach (var profile in expectedProfilesSequencesViews.Keys)
				{
					Assert.AreEqual(
						expectedProfilesSequencesViews[profile],
						profilesSequencesViews[profile],
						$"{profile} Profile");
				}
			});
		}

		[Test]
		public void NextIs_MultiProfiles_TargetItemsAreInTargetProfileOnly_Test()
		{
			var registry = new Registry();

			registry.For<I>().Add<A>();

			const int n = 7; // profiles.Length == regitrators.Length == 7;

			var regitrators = new Action<IProfileRegistry>[]
			{
				profile => profile.For<I>().Add<B>(),
				profile => profile.For<I>().Add<C>(),
				profile => profile.For<I>().Add<G>(),
				profile => profile.For<I>().Add<O>(),
				profile => profile.For<I>().Add<X>(),
				profile => profile.For<I>().Add<Z>(),
				profile => profile.For<I>().Add<E>()
			};

			string[] profiles = {"1", "22", "333", "4444", "55555", "666666", "7777777"};
			
			for (int i = 0; i < n; i++)
				registry.Profile(profiles[i], 
					regitrators.Take(n - i).Aggregate((acc, o) => acc + o));

			registry
				.ForEnumerabletOf<I>()
				.AddSequence()
				.NextIs<A>()
				.NextIs<B>().WhenProfileIsIn("1", "22", "333", "4444", "55555", "666666", "7777777")
				.NextIs<C>().WhenProfileIsIn("1", "22", "333", "4444", "55555", "666666")
				.NextIs<G>().WhenProfileIsIn("1", "22", "333", "4444", "55555")
				.NextIs<O>().WhenProfileIsIn("1", "22", "333", "4444")
				.NextIs<X>().WhenProfileIsIn("1", "22", "333")
				.NextIs<Z>().WhenProfileIsIn("1", "22")
				.NextIs<E>().WhenProfileIs("1")
				.End();

			var profilesSequencesViews = new Dictionary<string, string>();
			var expectedProfilesSequencesViews = new Dictionary<string, string>
				{
					{"1", "ABCGOXZE"},
					{"22", "ABCGOXZ"},
					{"333", "ABCGOX"},
					{"4444", "ABCGO"},
					{"55555", "ABCG"},
					{"666666", "ABC"},
					{"7777777", "AB"}
				};

			string defaultSequenceView;
			using (var container = new Container(registry))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				defaultSequenceView = SelectViewFrom(sequence);

				foreach (var profile in expectedProfilesSequencesViews.Keys)
					using (var profileContainer = container.GetNestedContainer(profile))
					{
						var profileSequence = profileContainer.GetInstance<IEnumerable<I>>();
						profilesSequencesViews.Add(profile, SelectViewFrom(profileSequence));
					}
			}

			Assert.Multiple(() =>
			{
				Assert.AreEqual("A", defaultSequenceView, "Default Profile");
				foreach (var profile in expectedProfilesSequencesViews.Keys)
				{
					Assert.AreEqual(
						expectedProfilesSequencesViews[profile],
						profilesSequencesViews[profile],
						$"{profile} Profile");
				}
			});
		}

		[Test]
		public void NextIs_EmptySequenceForDefaultProfile_TargetItemsAreInTargetProfileOnly_SingleProfile_Test()
		{
			var registry = new Registry();

			registry.Profile("Dog", profile =>
			{
				profile.For<I>().Add<A>();
				profile.For<I>().Add<C>();
				profile.For<I>().Add<B>();
			});

			registry
				.ForEnumerabletOf<I>()
				.UseSequence()
				.NextIs<A>().WhenProfileIs("Dog")
				.NextIs<B>().WhenProfileIs("Dog")
				.NextIs<C>().WhenProfileIs("Dog")
				.End();

			using (var container = new Container(registry))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();

				var defaultSequenceView = SelectViewFrom(sequence);
				var dogSequenceView = SelectViewFrom(dogSequence);

				Assert.Multiple(() =>
				{
					Assert.IsEmpty(defaultSequenceView, "Default Profile");
					Assert.AreEqual("ABC", dogSequenceView, "Dog Profile");
				});
			}
		}

	    [Test]
	    public void NextIs_EmptySequenceForDefaultProfile_SingleProfile_Test()
	    {
		    var registry = new Registry();

		    registry.For<I>().Add<B>();
		    registry.For<I>().Add<A>();
		    registry.For<I>().Add<C>();

		    registry
			    .ForEnumerabletOf<I>()
			    .UseSequence()
			    .NextIs<A>().WhenProfileIs("Dog")
			    .NextIs<B>().WhenProfileIs("Dog")
			    .NextIs<C>().WhenProfileIs("Dog")
			    .End();

		    using (var container = new Container(registry))
		    using (var dogContainer = container.GetNestedContainer("Dog"))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();

			    var defaultSequenceView = SelectViewFrom(sequence);
			    var dogSequenceView = SelectViewFrom(dogSequence);

			    Assert.Multiple(() =>
			    {
				    Assert.IsEmpty(defaultSequenceView, "Default Profile");
				    Assert.AreEqual("ABC", dogSequenceView, "Dog Profile");
			    });
		    }
	    }
		
	    public void NextIs_EmptySequenceForDefaultProfile_TwoProfiles_Test()
	    {
		    var registry = new Registry();

		    registry.For<I>().Add<L>();
		    registry.For<I>().Add<Y>();
		    registry.For<I>().Add<B>();
		    registry.For<I>().Add<R>();
		    registry.For<I>().Add<M>();
		    registry.For<I>().Add<M>();

		    registry
			    .ForEnumerabletOf<I>()
			    .UseSequence()
			    .NextIs<A>().WhenProfileIs("Dog")
			    .NextIs<B>().WhenProfileIs("Dog")
			    .NextIs<Y>().WhenProfileIsIn("Dog", "Cat")
			    .NextIs<L>().WhenProfileIsIn("Cat", "Dog")
			    .NextIs<M>().WhenProfileIs("Cat")
			    .NextIs<R>().WhenProfileIs("Cat")
			    .End();

		    using (var container = new Container(registry))
		    using (var dogContainer = container.GetNestedContainer("Dog"))
		    using (var catContainer = container.GetNestedContainer("Cat"))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();
			    var catSequence = catContainer.GetInstance<IEnumerable<I>>();

			    var defaultSequenceView = SelectViewFrom(sequence);
			    var dogSequenceView = SelectViewFrom(dogSequence);
			    var catSequenceView = SelectViewFrom(catSequence);

			    Assert.Multiple(() =>
			    {
				    Assert.IsEmpty(defaultSequenceView, "Default Profile");
				    Assert.AreEqual("ABYL", dogSequenceView, "Dog Profile");
				    Assert.AreEqual("YLMR", catSequenceView, "Cat Profile");
			    });
		    }
	    }

	    public void NextIs_EmptySequenceForDefaultProfile_TargetItemsAreInTargetProfilesOnly_TwoProfiles_Test()
	    {
		    var registry = new Registry();

			registry.Profile("Dog", profile =>
			{
				profile.For<I>().Add<Y>();
				profile.For<I>().Add<B>();
				profile.For<I>().Add<A>();
				profile.For<I>().Add<L>();
			});

		    registry.Profile("Cat", profile =>
		    {
			    profile.For<I>().Add<Y>();
			    profile.For<I>().Add<M>();
			    profile.For<I>().Add<R>();
			    profile.For<I>().Add<L>();
		    });

			registry.For<I>().Add<A>();
		    registry.For<I>().Add<B>();
		    registry.For<I>().Add<Y>();
		    registry.For<I>().Add<L>();
		    registry.For<I>().Add<M>();
		    registry.For<I>().Add<R>();

		    registry
			    .ForEnumerabletOf<I>()
			    .UseSequence()
			    .NextIs<A>().WhenProfileIs("Dog")
			    .NextIs<B>().WhenProfileIs("Dog")
			    .NextIs<Y>().WhenProfileIsIn("Dog", "Cat")
			    .NextIs<L>().WhenProfileIsIn("Cat", "Dog")
			    .NextIs<M>().WhenProfileIs("Cat")
			    .NextIs<R>().WhenProfileIs("Cat")
			    .End();

		    using (var container = new Container(registry))
		    using (var dogContainer = container.GetNestedContainer("Dog"))
		    using (var catContainer = container.GetNestedContainer("Cat"))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();
			    var catSequence = catContainer.GetInstance<IEnumerable<I>>();

			    var defaultSequenceView = SelectViewFrom(sequence);
			    var dogSequenceView = SelectViewFrom(dogSequence);
			    var catSequenceView = SelectViewFrom(catSequence);

			    Assert.Multiple(() =>
			    {
				    Assert.IsEmpty(defaultSequenceView, "Default Profile");
				    Assert.AreEqual("ABYL", dogSequenceView, "Dog Profile");
				    Assert.AreEqual("YLMR", catSequenceView, "Cat Profile");
			    });
		    }
	    }

	    public void NextIs_EmptySequenceForDefaultProfile_TargetItemsAreInTargetAndDefaultProfilesOnly_TwoProfiles_Test()
	    {
		    var registry = new Registry();

		    registry.For<I>().Add<Y>();
		    registry.For<I>().Add<L>();

			registry.Profile("Dog", profile =>
		    {
			    profile.For<I>().Add<B>();
			    profile.For<I>().Add<A>();
		    });

		    registry.Profile("Cat", profile =>
		    {
			    profile.For<I>().Add<M>();
			    profile.For<I>().Add<R>();
		    });

		    registry.For<I>().Add<A>();
		    registry.For<I>().Add<B>();
		    registry.For<I>().Add<Y>();
		    registry.For<I>().Add<L>();
		    registry.For<I>().Add<M>();
		    registry.For<I>().Add<R>();

		    registry
			    .ForEnumerabletOf<I>()
			    .UseSequence()
			    .NextIs<A>().WhenProfileIs("Dog")
			    .NextIs<B>().WhenProfileIs("Dog")
			    .NextIs<Y>().WhenProfileIsIn("Dog", "Cat")
			    .NextIs<L>().WhenProfileIsIn("Cat", "Dog")
			    .NextIs<M>().WhenProfileIs("Cat")
			    .NextIs<R>().WhenProfileIs("Cat")
			    .End();

		    using (var container = new Container(registry))
		    using (var dogContainer = container.GetNestedContainer("Dog"))
		    using (var catContainer = container.GetNestedContainer("Cat"))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();
			    var catSequence = catContainer.GetInstance<IEnumerable<I>>();

			    var defaultSequenceView = SelectViewFrom(sequence);
			    var dogSequenceView = SelectViewFrom(dogSequence);
			    var catSequenceView = SelectViewFrom(catSequence);

			    Assert.Multiple(() =>
			    {
				    Assert.IsEmpty(defaultSequenceView, "Default Profile");
				    Assert.AreEqual("ABYL", dogSequenceView, "Dog Profile");
				    Assert.AreEqual("YLMR", catSequenceView, "Cat Profile");
			    });
		    }
	    }

		[Test]
		public void NextIs_UseTwoNamedSequences_NoProfile_Test()
		{
			var registry = new Registry();

			registry.For<I>().Add<A>();
			registry.For<I>().Add<B>();
			registry.For<I>().Add<C>();
			registry.For<I>().Add<D>();
			registry.For<I>().Add<E>();
			registry.For<I>().Add<F>();
			registry.For<I>().Add<G>();
			registry.For<I>().Add<K>();

			registry
				.ForEnumerabletOf<I>()
				.UseSequence()
				.Named("Red")
				.NextIs<A>()
				.NextIs<B>()
				.NextIs<C>()
				.NextIs<D>()
				.NextIs<E>()
				.End();

			registry
				.ForEnumerabletOf<I>()
				.UseSequence()
				.Named("Blue")
				.NextIs<F>()
				.NextIs<G>()
				.NextIs<K>()
				.NextIs<A>()
				.NextIs<B>()
				.End();

			using (var container = new Container(registry))
			{
				var redSequence = container.GetInstance<IEnumerable<I>>("Red");
				var blueSequence = container.GetInstance<IEnumerable<I>>("Blue");
				var defaultSequence = container.GetInstance<IEnumerable<I>>();

				var redSequenceView = SelectViewFrom(redSequence);
				var blueSequenceView = SelectViewFrom(blueSequence);
				var defaultSequenceView = SelectViewFrom(defaultSequence);

				Assert.Multiple(() =>
				{
					Assert.AreEqual("ABCDE", redSequenceView, "Red Sequence");
					Assert.AreEqual("FGKAB", blueSequenceView, "Blue Sequence");
					Assert.AreEqual("FGKAB", defaultSequenceView, "Default Sequence");
				});
			}
		}

		[Test]
		public void NextIs_TwoNamedSequences_UseFirstAddSecond_NoProfile_Test()
		{
			var registry = new Registry();

			registry.For<I>().Add<A>();
			registry.For<I>().Add<B>();
			registry.For<I>().Add<C>();
			registry.For<I>().Add<D>();
			registry.For<I>().Add<E>();
			registry.For<I>().Add<F>();
			registry.For<I>().Add<G>();
			registry.For<I>().Add<K>();

			registry
				.ForEnumerabletOf<I>()
				.UseSequence()
				.Named("Red")
				.NextIs<A>()
				.NextIs<B>()
				.NextIs<C>()
				.NextIs<D>()
				.NextIs<E>()
				.End();

			registry
				.ForEnumerabletOf<I>()
				.AddSequence()
				.Named("Blue")
				.NextIs<F>()
				.NextIs<G>()
				.NextIs<K>()
				.NextIs<A>()
				.NextIs<B>()
				.End();

			using (var container = new Container(registry))
			{
				var redSequence = container.GetInstance<IEnumerable<I>>("Red");
				var blueSequence = container.GetInstance<IEnumerable<I>>("Blue");
				var defaultSequence = container.GetInstance<IEnumerable<I>>();

				var redSequenceView = SelectViewFrom(redSequence);
				var blueSequenceView = SelectViewFrom(blueSequence);
				var defaultSequenceView = SelectViewFrom(defaultSequence);

				Assert.Multiple(() =>
				{
					Assert.AreEqual("ABCDE", redSequenceView, "Red Sequence");
					Assert.AreEqual("FGKAB", blueSequenceView, "Blue Sequence");
					Assert.AreEqual("ABCDE", defaultSequenceView, "Default Sequence");
				});
			}
		}

		[Test]
		public void NextIs_TwoNamedSequences_TwoProfiles_NotIntersectedItems_Test()
		{
			var registry = new Registry();
			
			registry.For<I>().Add<A>();
			registry.For<I>().Add<B>();
			registry.For<I>().Add<C>();
			registry.For<I>().Add<X>();
			registry.For<I>().Add<Y>();

			registry
				.ForEnumerabletOf<I>()
				.AddSequence()
				.Named("Red")
				.NextIs<A>().WhenProfileIs("Dog")
				.NextIs<B>().WhenProfileIs("Cat")
				.NextIs<C>().WhenProfileIs("Dog")
				.NextIs<X>().WhenProfileIs("Dog")
				.NextIs<Y>().WhenProfileIs("Cat")
				.End();

			registry
				.ForEnumerabletOf<I>()
				.AddSequence()
				.Named("Blue")
				.NextIs<A>().WhenProfileIs("Cat")
				.NextIs<B>().WhenProfileIs("Dog")
				.NextIs<C>().WhenProfileIs("Cat")
				.NextIs<X>().WhenProfileIs("Cat")
				.NextIs<Y>().WhenProfileIs("Dog")
				.End();

			using (var container = new Container(registry))
			using (var catContainer = container.GetNestedContainer("Cat"))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			{
				var redSequence = container.GetInstance<IEnumerable<I>>("Red");
				var blueSequence = container.GetInstance<IEnumerable<I>>("Blue");
				var redCatSequence = catContainer.GetInstance<IEnumerable<I>>("Red");
				var blueCatSequence = catContainer.GetInstance<IEnumerable<I>>("Blue");
				var redDogSequence = dogContainer.GetInstance<IEnumerable<I>>("Red");
				var blueDogSequence = dogContainer.GetInstance<IEnumerable<I>>("Blue");

				var redCatResultView = SelectViewFrom(redCatSequence);
				var blueCatResultView = SelectViewFrom(blueCatSequence);
				var redDogResultView = SelectViewFrom(redDogSequence);
				var blueDogResultView = SelectViewFrom(blueDogSequence);

				Assert.Multiple(() =>
				{
					Assert.IsEmpty(redSequence, "Red Sequence, Default Profile");
					Assert.IsEmpty(blueSequence, "Blue Sequence, Default Profile");
					Assert.AreEqual("BY", redCatResultView, "Red Sequence, Cat Profile");
					Assert.AreEqual("ACX", blueCatResultView, "Blue Sequence, Cat Profile");
					Assert.AreEqual("ACX", redDogResultView, "Red Sequence, Dog Profile");
					Assert.AreEqual("BY", blueDogResultView, "Blue Sequence, Cat Profile");
				});
			}
		}

		[Test]
	    public void NextIs_Args_TwoProfiles_SameSequencesButDifferentArgsOfItems_Test()
	    {
		    var registry = new Registry();

		    registry
			    .For<Arg1>().Use<Arg1>()
			    .Ctor<string>().Is("1");
		    registry
			    .For<Arg2>().Use<Arg2>()
			    .Ctor<string>().Is("2");
		    registry
			    .For<Arg3>().Use<Arg3>()
			    .Ctor<string>().Is("3");

		    registry
			    .For<Arg1A>().Use<Arg1A>()
			    .Ctor<string>().Is("1A");
		    registry
			    .For<Arg2A>().Use<Arg2A>()
			    .Ctor<string>().Is("2A");
		    registry
			    .For<Arg3A>().Use<Arg3A>()
			    .Ctor<string>().Is("3A");

		    registry
			    .For<Arg1B>().Use<Arg1B>()
			    .Ctor<string>().Is("1B");
		    registry
			    .For<Arg2B>().Use<Arg2B>()
			    .Ctor<string>().Is("2B");
		    registry
			    .For<Arg3B>().Use<Arg3B>()
			    .Ctor<string>().Is("3B");

		    registry
			    .For<Arg1C>().Use<Arg1C>()
			    .Ctor<string>().Is("1C");
		    registry
			    .For<Arg2C>().Use<Arg2C>()
			    .Ctor<string>().Is("2C");
		    registry
			    .For<Arg3C>().Use<Arg3C>()
			    .Ctor<string>().Is("3C");
			
			registry.For<I>().Add<WithArgs>();
		    registry.For<I>().Add<WithArgsA>();
		    registry.For<I>().Add<WithArgsB>();
		    registry.For<I>().Add<WithArgsC>();

			registry.Profile("Dog", profile =>
			{
				profile
					.For<Arg1A>().Use<Arg1A>()
					.Ctor<string>().Is("1A_Dog");
				profile
					.For<Arg2A>().Use<Arg2A>()
					.Ctor<string>().Is("2A_Dog");
				profile
					.For<Arg3A>().Use<Arg3A>()
					.Ctor<string>().Is("3A_Dog");

				profile
					.For<Arg1B>().Use<Arg1B>()
					.Ctor<string>().Is("1B_Dog");

				profile
					.For<Arg1C>().Use<Arg1C>()
					.Ctor<string>().Is("1C_Dog");
				profile
					.For<Arg3C>().Use<Arg3C>()
					.Ctor<string>().Is("3C_Dog");
			});
			
			registry.ForEnumerabletOf<I>()
				.UseSequence()
				.NextIs<Arg1>()
				.NextIs<Arg1A>()
				.NextIs<Arg1C>()
				.NextIs<WithArgs>()
				.NextIs<WithArgsB>()
				.NextIs<WithArgsC>()
				.NextIs<Arg1B>()
				.NextIs<WithArgsA>()
				.NextIs<WithArgs>()
				.NextIs<Arg1C>()
				.NextIs<WithArgsB>()
				.NextIs<Arg2C>()
				.NextIs<Arg2B>()
				.NextIs<Arg2A>()
				.NextIs<Arg1A>()
				.End();

		    using (var container = new Container(registry))
		    using (var dogContainer = container.GetNestedContainer("Dog"))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();

			    var defaultSequenceView = SelectViewFrom(sequence);
			    var dogSequenceView = SelectViewFrom(dogSequence);

			    Assert.Multiple(() =>
			    {
					Assert.AreEqual(
						"11A1C[1|2|3][1B|2B|3B][1C|2C|3C]1B[1A|2A|3A][1|2|3]1C[1B|2B|3B]2C2B2A1A",
						defaultSequenceView,
						"Default Profile");

					Assert.AreEqual(
						"11A_Dog1C_Dog[1|2|3][1B_Dog|2B|3B][1C_Dog|2C|3C_Dog]1B_Dog" + 
						"[1A_Dog|2A_Dog|3A_Dog][1|2|3]1C_Dog[1B_Dog|2B|3B]2C2B2A_Dog1A_Dog", 
					    dogSequenceView, 
					    "Dog Profile");
			    });
		    }
		}

		[Test]
		public void NextIs_Args_TwoProfiles_DifferentSequencesAndDifferentArgsOfItems_Test()
		{
			var registry = new Registry();

			registry
				.For<Arg1>().Use<Arg1>()
				.Ctor<string>().Is("1");
			registry
				.For<Arg2>().Use<Arg2>()
				.Ctor<string>().Is("2");
			registry
				.For<Arg3>().Use<Arg3>()
				.Ctor<string>().Is("3");

			registry
				.For<Arg1A>().Use<Arg1A>()
				.Ctor<string>().Is("1A");
			registry
				.For<Arg2A>().Use<Arg2A>()
				.Ctor<string>().Is("2A");
			registry
				.For<Arg3A>().Use<Arg3A>()
				.Ctor<string>().Is("3A");

			registry
				.For<Arg1B>().Use<Arg1B>()
				.Ctor<string>().Is("1B");
			registry
				.For<Arg2B>().Use<Arg2B>()
				.Ctor<string>().Is("2B");
			registry
				.For<Arg3B>().Use<Arg3B>()
				.Ctor<string>().Is("3B");

			registry
				.For<Arg1C>().Use<Arg1C>()
				.Ctor<string>().Is("1C");
			registry
				.For<Arg2C>().Use<Arg2C>()
				.Ctor<string>().Is("2C");
			registry
				.For<Arg3C>().Use<Arg3C>()
				.Ctor<string>().Is("3C");

			registry.For<I>().Add<WithArgs>();
			registry.For<I>().Add<WithArgsA>();
			registry.For<I>().Add<WithArgsB>();
			registry.For<I>().Add<WithArgsC>();

			registry.Profile("Dog", profile =>
			{
				profile
					.For<Arg1A>().Use<Arg1A>()
					.Ctor<string>().Is("1A_Dog");
				profile
					.For<Arg2A>().Use<Arg2A>()
					.Ctor<string>().Is("2A_Dog");
				profile
					.For<Arg3A>().Use<Arg3A>()
					.Ctor<string>().Is("3A_Dog");

				profile
					.For<Arg1B>().Use<Arg1B>()
					.Ctor<string>().Is("1B_Dog");

				profile
					.For<Arg1C>().Use<Arg1C>()
					.Ctor<string>().Is("1C_Dog");
				profile
					.For<Arg3C>().Use<Arg3C>()
					.Ctor<string>().Is("3C_Dog");
			});

			registry.ForEnumerabletOf<I>()
				.UseSequence()
				.NextIs<Arg1>()
				.NextIs<Arg1A>()
				.NextIs<Arg1C>()
				.NextIs<WithArgs>()
				.NextIs<WithArgsB>().WhenProfileIs("Dog")
				.NextIs<WithArgsC>().WhenProfileIs("Dog")
				.NextIs<Arg1B>()
				.NextIs<WithArgsA>()
				.NextIs<WithArgs>().WhenProfileIs("Dog")
				.NextIs<Arg1C>().WhenProfileIs("Dog")
				.NextIs<WithArgsB>()
				.NextIs<Arg2C>()
				.NextIs<Arg2B>().WhenProfileIs("Dog")
				.NextIs<Arg2A>().WhenProfileIs("Dog")
				.NextIs<Arg1A>()
				.End();

			using (var container = new Container(registry))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();

				var defaultSequenceView = SelectViewFrom(sequence);
				var dogSequenceView = SelectViewFrom(dogSequence);

				Assert.Multiple(() =>
				{
					Assert.AreEqual(
						"11A1C[1|2|3]1B[1A|2A|3A][1B|2B|3B]2C1A",
						defaultSequenceView,
						"Default Profile");

					Assert.AreEqual(
						"11A_Dog1C_Dog[1|2|3][1B_Dog|2B|3B][1C_Dog|2C|3C_Dog]1B_Dog" +
						"[1A_Dog|2A_Dog|3A_Dog][1|2|3]1C_Dog[1B_Dog|2B|3B]2C2B2A_Dog1A_Dog",
						dogSequenceView,
						"Dog Profile");
				});
			}
		}

		[Test]
	    public void AddNext_NextIs_NoProfile_Test()
	    {
		    var registry = new Registry();

		    registry.For<I>().Add<A>();
		    registry.For<I>().Add<B>();
		    registry.For<I>().Add<C>();
		    registry.For<I>().Add<D>();
		    registry.For<I>().Add<E>();

			registry.ForEnumerabletOf<I>()
				.UseSequence()
				.AddNext<X>()
				.NextIs<A>()
				.AddNext<Y>()
				.NextIs<D>()
				.NextIs<B>()
				.AddNext<Z>()
				.AddNext<V>()
				.NextIs<C>()
				.NextIs<E>()
				.End();

		    using (var container = new Container(registry))
		    {
			    var sequence = container.GetInstance<IEnumerable<I>>();
			    var sequenceView = SelectViewFrom(sequence);
			    Assert.AreEqual("XAYDBZVCE", sequenceView);
			}
		}

	    [Test]
	    public void AddNext_NextIs_SingleProfile_Test()
	    {
		    var registry = new Registry();

		    registry.For<I>().Add<A>();
		    registry.For<I>().Add<B>();
		    registry.For<I>().Add<C>();

			registry.Profile("Dog",
				profile =>
				{
					profile.For<I>().Add<D>();
					profile.For<I>().Add<E>();
					profile.For<I>().Add<G>();
				});

		    registry.ForEnumerabletOf<I>()
			    .UseSequence()
			    .AddNext<X>()
			    .AddNext<Y>().IntoProfile("Dog")
			    .NextIs<A>()
				.NextIs<B>()
			    .NextIs<D>().WhenProfileIs("Dog")
				.AddNext<Z>()
				.AddNext<O>()
			    .NextIs<C>()
				.AddNext<S>().IntoProfile("Dog")
			    .AddNext<Q>().IntoProfile("Dog")
			    .NextIs<A>()
				.NextIs<E>().WhenProfileIs("Dog")
			    .NextIs<G>().WhenProfileIs("Dog")
			    .AddNext<Y>()
			    .NextIs<G>().WhenProfileIs("Dog")
			    .NextIs<E>().WhenProfileIs("Dog")
			    .AddNext<P>()
			    .NextIs<E>().WhenProfileIs("Dog")
			    .NextIs<A>()
				.End();

			using (var container = new Container(registry))
			using (var dogContainer = container.GetNestedContainer("Dog"))
			{
				var sequence = container.GetInstance<IEnumerable<I>>();
				var dogSequence = dogContainer.GetInstance<IEnumerable<I>>();

				var sequenceView = SelectViewFrom(sequence);
				var dogSequenceView = SelectViewFrom(dogSequence);

				Assert.Multiple(() =>
				{
					Assert.AreEqual("XABZOCAYPA", sequenceView, "Default Profile");
					Assert.AreEqual("XYABDZOCSQAEGYGEPEA", dogSequenceView, "Dog Profile");
				});
			}
	    }

		private string SelectViewFrom(IEnumerable<I> sequence) =>
		    string.Join(string.Empty, sequence.Select(o => o.Name));

		private interface I { string Name { get; } }
		private class A : I { public string Name => "A"; }
	    private class B : I { public string Name => "B"; }
		private class C : I { public string Name => "C"; }
		private class D : I { public string Name => "D"; }
		private class E : I { public string Name => "E"; }
		private class F : I { public string Name => "F"; }
		private class G : I { public string Name => "G"; }
		private class H : I { public string Name => "H"; }
		private class K : I { public string Name => "K"; }
		private class L : I { public string Name => "L"; }
		private class M : I { public string Name => "M"; }
		private class N : I { public string Name => "N"; }
		private class O : I { public string Name => "O"; }
		private class P : I { public string Name => "P"; }
		private class Q : I { public string Name => "Q"; }
		private class R : I { public string Name => "R"; }
		private class S : I { public string Name => "S"; }
		private class T : I { public string Name => "T"; }
		private class V : I { public string Name => "V"; }
		private class X : I { public string Name => "X"; }
		private class Y : I { public string Name => "Y"; }
		private class Z : I { public string Name => "Z"; }
		
		private class Triplett : I
		{
			public Triplett(I first, I second, I last)
			{
				Name = $"({first.Name}|{second.Name}|{last.Name})";
			}

			public string Name { get; }
		}

	    private class Arg : I
	    {
		    public Arg(string name) { Name = name; }

		    public string Name { get; }
	    }

		private class Arg1 : Arg { public Arg1(string name) : base(name) { } }
	    private class Arg2 : Arg { public Arg2(string name) : base(name) { } }
	    private class Arg3 : Arg { public Arg3(string name) : base(name) { } }

	    private class Arg1A : Arg1 { public Arg1A(string name) : base(name) { } }
	    private class Arg2A : Arg2 { public Arg2A(string name) : base(name) { } }
	    private class Arg3A : Arg3 { public Arg3A(string name) : base(name) { } }

	    private class Arg1B : Arg1 { public Arg1B(string name) : base(name) { } }
	    private class Arg2B : Arg2 { public Arg2B(string name) : base(name) { } }
	    private class Arg3B : Arg3 { public Arg3B(string name) : base(name) { } }

	    private class Arg1C : Arg1 { public Arg1C(string name) : base(name) { } }
	    private class Arg2C : Arg2 { public Arg2C(string name) : base(name) { } }
	    private class Arg3C : Arg3 { public Arg3C(string name) : base(name) { } }


		private class WithArgs : I
	    {
		    public WithArgs(Arg1 first, Arg2 second, Arg3 last)
		    {
			    Name = $"[{first.Name}|{second.Name}|{last.Name}]";
		    }

		    public string Name { get; }
	    }

	    private class WithArgsA : WithArgs
	    {
		    public WithArgsA(Arg1A first, Arg2A second, Arg3A last) 
			    : base(first, second, last) { }
	    }

	    private class WithArgsB : WithArgs
	    {
		    public WithArgsB(Arg1B first, Arg2B second, Arg3B last)
			    : base(first, second, last) { }
	    }

	    private class WithArgsC : WithArgs
	    {
		    public WithArgsC(Arg1C first, Arg2C second, Arg3C last)
			    : base(first, second, last) { }
	    }
	}
}
