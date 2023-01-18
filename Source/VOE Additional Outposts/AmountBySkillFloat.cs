using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld;
using Verse;

namespace VOEAdditionalOutposts
{
    public class AmountBySkillFloat
    {
        public float Count;

        public SkillDef Skill;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured AmountBySkill: " + xmlRoot.OuterXml);
                return;
            }
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef((object)this, "Skill", xmlRoot.Name);
            Count = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }

        public float Amount(List<Pawn> pawns)
        {
            return Count * pawns.Sum((Pawn p) => p.skills.GetSkill(Skill).Level);
        }
    }
}
