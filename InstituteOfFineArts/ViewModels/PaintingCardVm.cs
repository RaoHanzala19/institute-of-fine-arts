using InstituteOfFineArts.Domain;

namespace InstituteOfFineArts.ViewModels;

public class PaintingCardVm
{
    public Submission S { get; set; } = null!;
    public string Href { get; set; } = "#";
    public string Artist { get; set; } = "";
    public int Likes { get; set; }
    public bool Liked { get; set; }
    public bool Anonymous { get; set; }
    public bool ShowLike { get; set; } = true;
    public bool ShowGrade { get; set; }
    public string? FootLeft { get; set; }
    public string? FootRight { get; set; }
}