﻿using FemcConfig.Library.Config.Options;
namespace FemcConfig.Library.Config.Sections.Audio.Music;

public class BossMusic : ISection
{
    public string Name { get; } = "Boss Battle Music";

    public string Description { get; } = "Music used during some boss battles.";

    public SectionCategory Category { get; } = SectionCategory.Audio;

    public ModOption[] Options { get; }

    public BossMusic(AppService app)
    {
        var ctx = app.GetContext();
        this.Options =
        [
            new ModOption(ctx)
            {
                InternalName = "music_atlus_ms",
                Name = "Master Of Shadow Reload",
                Authors = [Author.Atlus],
                Enable = ctx => ctx.FemcConfig.Settings.Bossmusictrue = Models.FemcModConfig.bossmusic.MasterOfShadowReload,
                IsEnabledFunc = ctx => ctx.FemcConfig.Settings.Bossmusictrue == Models.FemcModConfig.bossmusic.MasterOfShadowReload,
            },
            new ModOption(ctx)
            {
                InternalName = "music_mosq_ms",
                Name = "Master Of Shadow Fate Mix",
                Authors = [Author.Mosq],
                Enable = ctx => ctx.FemcConfig.Settings.Bossmusictrue = Models.FemcModConfig.bossmusic.MasterOfShadowFateMixByMosq,
                IsEnabledFunc = ctx => ctx.FemcConfig.Settings.Bossmusictrue == Models.FemcModConfig.bossmusic.MasterOfShadowFateMixByMosq,
            },
        ];
    }
}
