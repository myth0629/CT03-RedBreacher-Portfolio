using System.Collections.Generic;
using UnityEngine;

public class SkinEditor_SkillLevel : MonoBehaviour
{
    public BodyPaint bodyPaint;
    public ChevronMark chevronMark;
    
    private readonly Dictionary<BodyPaint, Color> _camouflageColors = new Dictionary<BodyPaint, Color>
    {
        { BodyPaint.None, new Color32(200, 200, 255, 255) },
        { BodyPaint.BlueTape, new Color32(86,91, 104, 255) },
        { BodyPaint.FrameIce, new Color32(173,196, 255, 255) },
        { BodyPaint.DarkPurple, new Color32(177,135, 255, 255) },
        { BodyPaint.CrimsonRubby, new Color32(186, 80, 89, 255) },
        { BodyPaint.BlackPowder, new Color32(99, 99, 99, 255) },
        { BodyPaint.Bronze, new Color32(255, 165, 114, 255) },
        { BodyPaint.Silver, new Color32(237, 255, 255, 255) },
        { BodyPaint.Gold, new Color32(255, 222, 102, 255) }
    };

    private readonly Dictionary<ChevronMark, Color> _chevronColors = new Dictionary<ChevronMark, Color>
    {
        { ChevronMark.None, new Color32(255, 0, 50, 255) },
        { ChevronMark.YelloOrange, new Color32(231,138, 0, 255) },
        { ChevronMark.WoodMint, new Color32(100,171, 148, 255) },
        { ChevronMark.BlueTape, new Color32(0,58, 195, 255) },
        { ChevronMark.DarkPurple, new Color32(75,0, 135, 255) },
        { ChevronMark.FrameIce, new Color32(255,128, 88, 255) },
        { ChevronMark.CrimsonRubby, new Color32(191, 39, 58, 255) },
        { ChevronMark.BlackPowder, new Color32(39, 39, 39, 255) },
        { ChevronMark.Bronze, new Color32(255, 165, 114, 255) },
        { ChevronMark.Silver, new Color32(237, 255, 255, 255) },
        { ChevronMark.Gold, new Color32(255, 222, 102, 255) }
    };

    public void OnValidate()
    {
        foreach (var spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
        {
            if (spriteRenderer.name.Contains("Wheel") || spriteRenderer.name.Contains("Track") || spriteRenderer.name.Contains("[NoPaint]")) continue;

            if (spriteRenderer.name.Contains("Chevron"))
            {
                spriteRenderer.color = _chevronColors[chevronMark];
            }
            else
            {
                spriteRenderer.color = _camouflageColors[bodyPaint];
            }
        }
    }
    
    public enum BodyPaint
    {
        None,
        BlueTape,
        FrameIce,
        DarkPurple,
        CrimsonRubby,
        BlackPowder,
        Bronze,
        Silver,
        Gold,
    }

    public enum ChevronMark
    {
        None,
        YelloOrange,
        WoodMint,
        BlueTape,
        FrameIce,
        DarkPurple,
        CrimsonRubby,
        BlackPowder,
        Bronze,
        Silver,
        Gold,
    }
}
