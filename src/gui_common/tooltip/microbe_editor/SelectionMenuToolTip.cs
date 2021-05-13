﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Godot;

/// <summary>
///   The main tooltip class for the selections on the microbe editor's selection menu.
///   Contains list of processes and modifiers info.
/// </summary>
public class SelectionMenuToolTip : Control, ICustomToolTip
{
    [Export]
    public NodePath NameLabelPath;

    [Export]
    public NodePath MpLabelPath;

    [Export]
    public NodePath DescriptionLabelPath;

    [Export]
    public NodePath ProcessesDescriptionLabelPath;

    [Export]
    public NodePath ModifierListPath;

    [Export]
    public NodePath ProcessListPath;

    private PackedScene modifierInfoScene;

    private Label nameLabel;
    private Label mpLabel;

    private Label descriptionLabel;
    private RichTextLabel processesDescriptionLabel;
    private VBoxContainer modifierInfoList;
    private VBoxContainer processList;

    private string displayName;
    private string description;
    private string processesDescription;
    private int mpCost;

    /// <summary>
    ///   Hold reference of modifier info elements for easier access to change their values later
    /// </summary>
    private List<ModifierInfoLabel> modifierInfos = new List<ModifierInfoLabel>();

    public Vector2 Position
    {
        get => RectPosition;
        set => RectPosition = value;
    }

    public Vector2 Size
    {
        get => RectSize;
        set => RectSize = value;
    }

    [Export]
    public string DisplayName
    {
        get => displayName;
        set
        {
            displayName = value;
            UpdateName();
        }
    }

    /// <summary>
    ///   Description of processes an organelle does if any.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This supports custom format (for example: "Turns [glucose] into [atp]") where strings inside
    ///     the square brackets will be parsed and replaced with a matching compound name and icon. This
    ///     is done to make translating feasible.
    ///     NOTE: description string should only be set here and not directly on the rich text label node
    ///     as it will be overidden otherwise.
    ///   </para>
    /// </remarks>
    [Export]
    public string ProcessesDescription
    {
        get => processesDescription;
        set
        {
            processesDescription = value;
            UpdateProcessesDescription();
        }
    }

    [Export]
    public string Description
    {
        get => description;
        set
        {
            description = value;
            UpdateDescription();
        }
    }

    [Export]
    public int MutationPointCost
    {
        get => mpCost;
        set
        {
            mpCost = value;
            UpdateMpCost();
        }
    }

    [Export]
    public float DisplayDelay { get; set; } = 0.0f;

    public bool ToolTipVisible
    {
        get => Visible;
        set => Visible = value;
    }

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.FollowMousePosition;

    public bool HideOnMousePress { get; set; } = false;

    public Node ToolTipNode => this;

    public override void _Ready()
    {
        nameLabel = GetNode<Label>(NameLabelPath);
        mpLabel = GetNode<Label>(MpLabelPath);
        descriptionLabel = GetNode<Label>(DescriptionLabelPath);
        processesDescriptionLabel = GetNode<RichTextLabel>(ProcessesDescriptionLabelPath);
        modifierInfoList = GetNode<VBoxContainer>(ModifierListPath);
        processList = GetNode<VBoxContainer>(ProcessListPath);

        modifierInfoScene = GD.Load<PackedScene>("res://src/gui_common/tooltip/microbe_editor/ModifierInfoLabel.tscn");

        UpdateName();
        UpdateDescription();
        UpdateProcessesDescription();
        UpdateMpCost();
        UpdateLists();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateProcessesDescription();
        }
    }

    /// <summary>
    ///   Instances the UI element for a modifier info
    /// </summary>
    public void AddModifierInfo(string name, float value)
    {
        var modifierInfo = (ModifierInfoLabel)modifierInfoScene.Instance();

        modifierInfo.DisplayName = name;
        modifierInfo.ModifierValue = value.ToString(CultureInfo.CurrentCulture);

        modifierInfoList.AddChild(modifierInfo);
        modifierInfos.Add(modifierInfo);
    }

    public ModifierInfoLabel GetModifierInfo(string nodeName)
    {
        return modifierInfos.Find(found => found.Name == nodeName);
    }

    /// <summary>
    ///   Creates UI elements for the processes info in a specific patch
    /// </summary>
    public void WriteOrganelleProcessList(List<ProcessSpeedInformation> processes)
    {
        // Remove previous process list
        processList.QueueFreeChildren();

        if (processes == null)
        {
            var noProcesslabel = new Label();
            noProcesslabel.Text = "No processes";
            processList.AddChild(noProcesslabel);
            return;
        }

        // TODO: change this to use ChemicalEquation
        foreach (var process in processes)
        {
            var processContainer = new VBoxContainer();
            processContainer.MouseFilter = MouseFilterEnum.Ignore;
            processList.AddChild(processContainer);

            var processTitle = new Label();
            processTitle.AddColorOverride("font_color", new Color(1.0f, 0.84f, 0.0f));
            processTitle.Text = process.Process.Name;
            processContainer.AddChild(processTitle);

            var processBody = new HBoxContainer();
            processBody.MouseFilter = MouseFilterEnum.Ignore;

            bool usePlus;

            if (process.OtherInputs.Count == 0)
            {
                // Just environmental stuff
                usePlus = true;
            }
            else
            {
                // Something turns into something else, uses the arrow notation
                usePlus = false;

                // Show the inputs
                // TODO: add commas or maybe pluses for multiple inputs
                foreach (var key in process.OtherInputs.Keys)
                {
                    var inputCompound = process.OtherInputs[key];

                    var amountLabel = new Label();
                    amountLabel.Text = Math.Round(inputCompound.Amount, 3) + " ";
                    processBody.AddChild(amountLabel);
                    processBody.AddChild(GUICommon.Instance.CreateCompoundIcon(inputCompound.Compound.InternalName));
                }

                // And the arrow
                var arrow = new TextureRect();
                arrow.Expand = true;
                arrow.RectMinSize = new Vector2(20, 20);
                arrow.Texture = GD.Load<Texture>("res://assets/textures/gui/bevel/WhiteArrow.png");
                processBody.AddChild(arrow);
            }

            // Outputs of the process. It's assumed that every process has outputs
            foreach (var key in process.Outputs.Keys)
            {
                var outputCompound = process.Outputs[key];

                var amountLabel = new Label();

                var stringBuilder = new StringBuilder(string.Empty, 150);

                // Changes process title and process# to red if process has 0 output
                if (outputCompound.Amount == 0)
                {
                    processTitle.AddColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
                    amountLabel.AddColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
                }

                if (usePlus)
                {
                    stringBuilder.Append(outputCompound.Amount >= 0 ? "+" : string.Empty);
                }

                stringBuilder.Append(Math.Round(outputCompound.Amount, 3) + " ");

                amountLabel.Text = stringBuilder.ToString();

                processBody.AddChild(amountLabel);
                processBody.AddChild(GUICommon.Instance.CreateCompoundIcon(outputCompound.Compound.InternalName));
            }

            var perSecondLabel = new Label();
            perSecondLabel.Text = TranslationServer.Translate("PER_SECOND_SLASH");

            processBody.AddChild(perSecondLabel);

            // Environment conditions
            if (process.EnvironmentInputs.Count > 0)
            {
                var atSymbol = new Label();

                atSymbol.Text = "@";
                atSymbol.RectMinSize = new Vector2(30, 20);
                atSymbol.Align = Label.AlignEnum.Center;
                processBody.AddChild(atSymbol);

                var first = true;

                foreach (var key in process.EnvironmentInputs.Keys)
                {
                    if (!first)
                    {
                        var commaLabel = new Label();
                        commaLabel.Text = ", ";
                        processBody.AddChild(commaLabel);
                    }

                    first = false;

                    var environmentCompound = process.EnvironmentInputs[key];

                    // To percentage
                    var percentageLabel = new Label();

                    // TODO: sunlight needs some special handling (it used to say the lux amount)
                    percentageLabel.Text = Math.Round(environmentCompound.AvailableAmount * 100, 1) + "%";

                    processBody.AddChild(percentageLabel);
                    processBody.AddChild(
                        GUICommon.Instance.CreateCompoundIcon(environmentCompound.Compound.InternalName));
                }
            }

            processContainer.AddChild(processBody);
        }
    }

    /// <summary>
    ///   Sets the value of all the membrane type modifiers on this tooltip relative
    ///   to the referenceMembrane. This currently only reads from the preadded modifier
    ///   UI elements on this tooltip and doesn't actually create them on runtime.
    /// </summary>
    public void WriteMembraneModifierList(MembraneType referenceMembrane, MembraneType membraneType)
    {
        foreach (var modifier in modifierInfos)
        {
            var deltaValue = 0.0f;

            switch (modifier.Name)
            {
                case "mobility":
                    deltaValue = membraneType.MovementFactor - referenceMembrane.MovementFactor;
                    break;
                case "osmoregulation_cost":
                    deltaValue = membraneType.OsmoregulationFactor - referenceMembrane.OsmoregulationFactor;
                    break;
                case "resource_absorption_speed":
                    deltaValue = membraneType.ResourceAbsorptionFactor - referenceMembrane.ResourceAbsorptionFactor;
                    break;
                case "health":
                    deltaValue = membraneType.Hitpoints - referenceMembrane.Hitpoints;
                    break;
                case "physical_resistance":
                    deltaValue = membraneType.PhysicalResistance - referenceMembrane.PhysicalResistance;
                    break;
                case "toxin_resistance":
                    deltaValue = membraneType.ToxinResistance - referenceMembrane.ToxinResistance;
                    break;
            }

            // All stats with +0 value that are not part of the selected membrane is made hidden
            // on the tooltip so it'll be easier to digest and compare modifier changes
            if (Name != referenceMembrane.InternalName && modifier.ShowValue)
                modifier.Visible = deltaValue != 0;

            // Apply the value to the text labels as percentage (except for Health)
            if (modifier.Name == "health")
            {
                modifier.ModifierValue = (deltaValue >= 0 ? "+" : string.Empty)
                    + deltaValue.ToString("F0", CultureInfo.CurrentCulture);
            }
            else
            {
                modifier.ModifierValue = ((deltaValue >= 0) ? "+" : string.Empty)
                    + (deltaValue * 100).ToString("F0", CultureInfo.CurrentCulture) + "%";
            }

            if (modifier.Name == "osmoregulation_cost")
            {
                modifier.AdjustValueColor(deltaValue, true);
            }
            else
            {
                modifier.AdjustValueColor(deltaValue);
            }
        }
    }

    public void OnDisplay()
    {
        Show();
    }

    public void OnHide()
    {
        Hide();
    }

    private string ParseProcessesDescription()
    {
        if (string.IsNullOrEmpty(ProcessesDescription))
            return string.Empty;

        // Parse compound names
        var result = Regex.Replace(TranslationServer.Translate(ProcessesDescription), @"\[(.*?)\]", found =>
        {
            // Just return the string as is if compound is not valid
            if (!SimulationParameters.Instance.DoesCompoundExist(found.Groups[1].Value))
                return found.Value;

            var compound = SimulationParameters.Instance.GetCompound(found.Groups[1].Value);

            return $"[b]{compound.Name}[/b] [font=res://src/gui_common/fonts/" +
                $"BBCode-Image-VerticalCenterAlign.tres] [img=25]{compound.IconPath}[/img][/font]";
        });

        return result;
    }

    private void UpdateName()
    {
        if (nameLabel == null)
            return;

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = nameLabel.Text;
        }
        else
        {
            nameLabel.Text = displayName;
        }
    }

    private void UpdateDescription()
    {
        if (descriptionLabel == null)
            return;

        if (string.IsNullOrEmpty(Description))
        {
            description = descriptionLabel.Text;
        }
        else
        {
            descriptionLabel.Text = description;
        }
    }

    private void UpdateProcessesDescription()
    {
        if (processesDescriptionLabel == null)
            return;

        processesDescriptionLabel.BbcodeText = ParseProcessesDescription();
    }

    private void UpdateMpCost()
    {
        if (mpLabel == null)
            return;

        mpLabel.Text = MutationPointCost.ToString();
    }

    private void UpdateLists()
    {
        foreach (ModifierInfoLabel item in modifierInfoList.GetChildren())
        {
            modifierInfos.Add(item);
        }
    }
}
