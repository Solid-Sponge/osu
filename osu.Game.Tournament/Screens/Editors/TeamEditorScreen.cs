// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osu.Game.Users;
using osuTK;

namespace osu.Game.Tournament.Screens.Editors
{
    public class TeamEditorScreen : TournamentEditorScreen<TeamEditorScreen.TeamRow>
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            foreach (var t in LadderInfo.Teams)
                Flow.Add(new TeamRow(t));
        }

        protected override void AddNew()
        {
            var team = new TournamentTeam();

            Flow.Add(new TeamRow(team));
            LadderInfo.Teams.Add(team);
        }

        public class TeamRow : CompositeDrawable
        {
            public readonly TournamentTeam Team;

            private readonly Container drawableContainer;

            [Resolved]
            private LadderInfo ladderInfo { get; set; }

            public TeamRow(TournamentTeam team)
            {
                Team = team;

                Masking = true;
                CornerRadius = 10;

                PlayerEditor playerEditor = new PlayerEditor(Team)
                {
                    Width = 0.95f
                };

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = OsuColour.Gray(0.1f),
                        RelativeSizeAxes = Axes.Both,
                    },
                    drawableContainer = new Container
                    {
                        Size = new Vector2(100, 50),
                        Margin = new MarginPadding(10),
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                    },
                    new FillFlowContainer
                    {
                        Margin = new MarginPadding(5),
                        Spacing = new Vector2(5),
                        Direction = FillDirection.Full,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new SettingsTextBox
                            {
                                LabelText = "Name",
                                Width = 0.2f,
                                Bindable = Team.FullName
                            },
                            new SettingsTextBox
                            {
                                LabelText = "Acronym",
                                Width = 0.2f,
                                Bindable = Team.Acronym
                            },
                            new SettingsTextBox
                            {
                                LabelText = "Flag",
                                Width = 0.2f,
                                Bindable = Team.FlagName
                            },
                            new SettingsButton
                            {
                                Width = 0.11f,
                                Margin = new MarginPadding(10),
                                Text = "Add player",
                                Action = () => playerEditor.CreateNew()
                            },
                            new DangerousSettingsButton
                            {
                                Width = 0.11f,
                                Text = "Delete Team",
                                Margin = new MarginPadding(10),
                                Action = () =>
                                {
                                    Expire();
                                    ladderInfo.Teams.Remove(Team);
                                },
                            },
                            playerEditor
                        }
                    },
                };

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                Team.FlagName.BindValueChanged(updateDrawable, true);
            }

            private void updateDrawable(ValueChangedEvent<string> flag)
            {
                drawableContainer.Child = new DrawableTeamFlag(Team);
            }

            private class DrawableTeamFlag : DrawableTournamentTeam
            {
                public DrawableTeamFlag(TournamentTeam team)
                    : base(team)
                {
                    InternalChild = Flag;
                    RelativeSizeAxes = Axes.Both;

                    Flag.Anchor = Anchor.Centre;
                    Flag.Origin = Anchor.Centre;
                }
            }

            public class PlayerEditor : CompositeDrawable
            {
                private readonly TournamentTeam team;
                private readonly FillFlowContainer flow;

                public PlayerEditor(TournamentTeam team)
                {
                    this.team = team;

                    RelativeSizeAxes = Axes.X;
                    AutoSizeAxes = Axes.Y;

                    InternalChild = flow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        LayoutDuration = 200,
                        LayoutEasing = Easing.OutQuint,
                        ChildrenEnumerable = team.Players.Select(p => new PlayerRow(team, p))
                    };
                }

                public void CreateNew()
                {
                    var user = new User();
                    team.Players.Add(user);
                    flow.Add(new PlayerRow(team, user));
                }

                public class PlayerRow : CompositeDrawable
                {
                    private readonly User user;

                    [Resolved]
                    protected IAPIProvider API { get; private set; }

                    private readonly Bindable<string> userId = new Bindable<string>();

                    private readonly Container drawableContainer;

                    public PlayerRow(TournamentTeam team, User user)
                    {
                        this.user = user;

                        Margin = new MarginPadding(10);

                        RelativeSizeAxes = Axes.X;
                        AutoSizeAxes = Axes.Y;

                        Masking = true;
                        CornerRadius = 5;

                        InternalChildren = new Drawable[]
                        {
                            new Box
                            {
                                Colour = OsuColour.Gray(0.2f),
                                RelativeSizeAxes = Axes.Both,
                            },
                            new FillFlowContainer
                            {
                                Margin = new MarginPadding(5),
                                Padding = new MarginPadding { Right = 160 },
                                Spacing = new Vector2(5),
                                Direction = FillDirection.Horizontal,
                                AutoSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new SettingsTextBox
                                    {
                                        LabelText = "User ID",
                                        RelativeSizeAxes = Axes.None,
                                        Width = 200,
                                        Bindable = userId,
                                    },
                                    drawableContainer = new Container
                                    {
                                        Size = new Vector2(100, 70),
                                    },
                                }
                            },
                            new DangerousSettingsButton
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                RelativeSizeAxes = Axes.None,
                                Width = 150,
                                Text = "Delete Player",
                                Action = () =>
                                {
                                    Expire();
                                    team.Players.Remove(user);
                                },
                            }
                        };
                    }

                    [BackgroundDependencyLoader]
                    private void load()
                    {
                        userId.Value = user.Id.ToString();
                        userId.BindValueChanged(idString =>
                        {
                            long parsed;

                            long.TryParse(idString.NewValue, out parsed);

                            user.Id = parsed;

                            if (idString.NewValue != idString.OldValue)
                                user.Username = string.Empty;

                            if (!string.IsNullOrEmpty(user.Username))
                            {
                                updatePanel();
                                return;
                            }

                            var req = new GetUserRequest(user.Id);

                            req.Success += res =>
                            {
                                // TODO: this should be done in a better way.
                                user.Username = res.Username;
                                user.Country = res.Country;
                                user.Cover = res.Cover;

                                updatePanel();
                            };

                            req.Failure += _ =>
                            {
                                user.Id = 1;
                                updatePanel();
                            };

                            API.Queue(req);
                        }, true);
                    }

                    private void updatePanel()
                    {
                        drawableContainer.Child = new UserPanel(user) { Width = 300 };
                    }
                }
            }
        }
    }
}
