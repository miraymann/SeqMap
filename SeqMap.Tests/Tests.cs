using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
			    .AddNext<H>("Dog")
			    .AddNext<G>()
			    .AddNext<O>("Dog")
				.AddNext<X>("Dog")
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
			    .AddNext<B>("1", "22", "333", "4444", "55555", "666666", "7777777")
			    .AddNext<C>("1", "22", "333", "4444", "55555", "666666")
			    .AddNext<G>("1", "22", "333", "4444", "55555")
			    .AddNext<O>("1", "22", "333", "4444")
			    .AddNext<X>("1", "22", "333")
			    .AddNext<Z>("1", "22")
			    .AddNext<E>("1")
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
			    .AddNext<A>("Dog")
			    .AddNext<B>("Dog")
			    .AddNext<C>("Dog")
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
			    .AddNext<A>("Dog")
			    .AddNext<B>("Dog")
			    .AddNext<Y>("Dog", "Cat")
			    .AddNext<L>("Cat", "Dog")
			    .AddNext<M>("Cat")
			    .AddNext<R>("Cat")
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
			    .AddNext<A>("Dog")
			    .AddNext<B>("Cat")
			    .AddNext<C>("Dog")
			    .AddNext<X>("Dog")
			    .AddNext<Y>("Cat")
			    .End();

		    registry
			    .ForEnumerabletOf<I>()
				.AddSequence()
			    .Named("Blue")
			    .AddNext<A>("Cat")
			    .AddNext<B>("Dog")
			    .AddNext<C>("Cat")
			    .AddNext<X>("Cat")
			    .AddNext<Y>("Dog")
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
			    .NextIsNamed("SSS", "Dog")
			    .NextIsNamed("PPP")
			    .NextIsNamed("SSS")
			    .NextIsNamed("SSS")
			    .NextIsNamed("QQQ", "Dog")
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
			    .NextIsNamed("SSS", "Dog")
			    .NextIsNamed("PPP", "Dog", "Cat")
			    .NextIsNamed("SSS", "Dog", "Cat")
			    .NextIsNamed("SSS")
			    .NextIsNamed("QQQ", "Dog")
				.NextIsNamed("PPP")
				.NextIsNamed("MMM")
				.NextIsNamed("SSS", "Dog", "Cat")
				.NextIsNamed("TTT", "Dog", "Cat")
				.NextIsNamed("XXX", "Dog")
				.NextIsNamed("SSS", "Cat")
				.NextIsNamed("PPP")
				.NextIsNamed("TTT")
				.NextIsNamed("TTT", "Cat")
				.NextIsNamed("TTT")
				.NextIsNamed("SSS", "Dog")
				.NextIsNamed("XXX")
				.NextIsNamed("PPP")
				.NextIsNamed("SSS", "Dog")
				.NextIsNamed("MMM", "Dog", "Cat")
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
			private readonly I _first, _second, _last;

			public Triplett(I first, I second, I last)
			{
				_first = first;
				_second = second;
				_last = last;
			}

			public string Name =>
				$"({_first.Name}|{_second.Name}|{_last.Name})";
		}
	}
}
