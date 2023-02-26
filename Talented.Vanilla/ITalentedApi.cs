using Shockah.Kokoro.Stardew.Skill;
using Shockah.Kokoro.UI;
using System;
using System.Collections.Generic;

namespace Shockah.Talented.Vanilla
{
	public interface ITalentedApi
	{
		IRequirementFactories RequirementFactories { get; }

		void RegisterTalentTag(ITalentTag tag);
		void UnregisterTalentTag(ITalentTag tag);

		void RegisterTalent(ITalent talent);
		void UnregisterTalent(ITalent talent);

		public interface IRequirementFactories
		{
			ITalentRequirements Talent(string talentUniqueID);
			ITalentRequirements Talent(ITalent talent);

			ITalentRequirements Tag(string tagUniqueID, int count);
			ITalentRequirements Tag(ITalentTag tag, int count);

			ITalentRequirements All(params ITalentRequirements[] requirements);
			ITalentRequirements All(IEnumerable<ITalentRequirements> requirements);

			ITalentRequirements Any(params ITalentRequirements[] requirements);
			ITalentRequirements Any(IEnumerable<ITalentRequirements> requirements);
		}
	}

	public interface ITalentTag
	{
		string UniqueID { get; }
		TextureRectangle Icon { get; }
		string Name { get; }
		ITalentTag? Parent => null;
		ISkill? Skill => null;
	}

	public record BasicTalentTag(
		string UniqueID,
		Func<TextureRectangle> IconProvider,
		Func<string> NameProvider,
		ITalentTag? Parent = null
	) : ITalentTag
	{
		public TextureRectangle Icon
			=> IconProvider();

		public string Name
			=> NameProvider();
	}

	public record SkillTalentTag(
		string UniqueID,
		ISkill Skill
	) : ITalentTag
	{
		public TextureRectangle Icon
			=> Skill.Icon!;

		public string Name
			=> Skill.Name;
	}

	public interface ITalent
	{
		string UniqueID { get; }
		TextureRectangle Icon { get; }
		string Name { get; }
		string Description { get; }
		ITalent? ReplacedTalent { get; }
		ITalentTag Tag { get; }
		ITalentRequirements? Requirements { get; }
		int PointCost => 1;

		bool Matches(ITalentTag tag)
		{
			ITalentTag? current = Tag;
			while (true)
			{
				if (tag.Equals(current))
					return true;
				current = current.Parent;
				if (current == null)
					return false;
			}
		}
	}

	public record Talent(
		string UniqueID,
		Func<TextureRectangle> IconProvider,
		Func<string> NameProvider,
		Func<string> DescriptionProvider,
		ITalent? ReplacedTalent,
		ITalentTag Tag,
		ITalentRequirements? Requirements,
		int PointCost = 1
	) : ITalent
	{
		public TextureRectangle Icon
			=> IconProvider();

		public string Name
			=> NameProvider();

		public string Description
			=> DescriptionProvider();
	}

	public interface ITalentRequirements
	{
		bool AreSatisifed(IEnumerable<ITalent> talents);
	}
}