﻿using FemcConfig.Library.Config.Options;

namespace FemcConfig.Library.Config.Sections;

public class KyotoPhotoSection : ISection
{
    public string Name { get; } = "Kyoto Photos";

    public string Description { get; } = "The photos taken in Kyoto ingame.";

    public SectionCategory Category { get; } = SectionCategory.TwoD;

    public ModOption[] Options { get; }

    public KyotoPhotoSection(AppService app)
    {
        var ctx = app.GetContext();
        this.Options =
        [
            new ModOption(ctx)
            {
                InternalName = "kyoto_ely",
                Authors = [Author.Ely],
                Enable = (ctx) => ctx.FemcConfig.Settings.KyotoEventTrue = Models.FemcModConfig.KyotoEventtype.ely,
                IsEnabledFunc = (ctx) => ctx.FemcConfig.Settings.KyotoEventTrue == Models.FemcModConfig.KyotoEventtype.ely,
            },
        ];
    }
}