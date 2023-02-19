using Shockah.Kokoro.Stardew.Skill;
using Shockah.Kokoro.UI;
using System;
using System.Collections.Generic;

namespace Shockah.Talented.Vanilla
{
	public interface ITalentedApi
	{
		void RegisterTalentTag(ITalentTag tag);
		void UnregisterTalentTag(ITalentTag tag);

		void RegisterTalent(ITalent talent);
		void UnregisterTalent(ITalent talent);
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
		string Name { get; }
		ITalent? ReplacedTalent { get; }
		ITalentTag Tag { get; }
		ITalentRequirements? Requirements { get; }
		int PointCost => 1;

		bool Matches(ITalentTag tag)
		{
			ITalentTag? current = Tag;
			while (true)
			{
				if (current == tag)
					return true;
				current = tag.Parent;
				if (current == null)
					return false;
			}
		}
	}

	public record Talent(
		string UniqueID,
		Func<string> NameProvider,
		ITalent? ReplacedTalent,
		ITalentTag Tag,
		ITalentRequirements? Requirements,
		int PointCost = 1
	) : ITalent
	{
		public string Name
			=> NameProvider();
	}

	public interface ITalentRequirements
	{
		bool AreSatisifed(IEnumerable<ITalent> talents);
	}
}