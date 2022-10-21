using UnityEngine;
using Verse;

namespace VOEAdditionalOutposts
{
    [StaticConstructorOnStartup]
    public static class TexOutposts
    {
        public static readonly Texture2D ImprisonTex = ContentFinder<Texture2D>.Get("Icons/BorderPostImprison");
    }
}
