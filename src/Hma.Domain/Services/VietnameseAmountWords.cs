using System.Globalization;
using System.Text;

namespace Hma.Domain.Services;

/// <summary>
/// Ports Nil.Utility.DocSo / DocBa / DocMot for invoice "bằng chữ".
/// </summary>
public static class VietnameseAmountWords
{
    public static string ToWords(decimal amount)
    {
        var text = DocSo(amount.ToString("0", CultureInfo.InvariantCulture));
        return string.Join(" ", text.Split(' ', StringSplitOptions.RemoveEmptyEntries)) + " đồng";
    }

    private static string DocMot(double so) => so switch
    {
        1 => " một ",
        2 => " hai ",
        3 => " ba ",
        4 => " bốn ",
        5 => " năm ",
        6 => " sáu ",
        7 => " bảy ",
        8 => " tám ",
        9 => " chín ",
        _ => ""
    };

    private static string DocBa(int baSo)
    {
        var strBaSo = baSo.ToString(CultureInfo.InvariantCulture);
        var nso = strBaSo.Length;
        int tram = 0, chuc = 0, donVi = 0;
        if (nso == 3)
        {
            tram = int.Parse(strBaSo[..1]);
            chuc = int.Parse(strBaSo.Substring(1, 1));
            donVi = int.Parse(strBaSo[^1..]);
        }
        else if (nso == 2)
        {
            chuc = int.Parse(strBaSo[..1]);
            donVi = int.Parse(strBaSo[^1..]);
        }
        else if (nso == 1)
        {
            donVi = int.Parse(strBaSo);
        }

        var str = new StringBuilder();
        if (tram > 0 && chuc == 0 && donVi > 0)
            str.Append(DocMot(tram)).Append(" trăm linh ");
        else if (tram > 0)
            str.Append(DocMot(tram)).Append(" trăm ");

        if (chuc == 1) str.Append(" mười ");
        else if (chuc > 1) str.Append(DocMot(chuc)).Append(" mươi ");

        if (chuc > 0 && donVi == 5) str.Append(" lăm ");
        else if (chuc > 1 && donVi == 1) str.Append(" mốt ");
        else if (chuc > 1 && donVi == 4) str.Append(" tư ");
        else if (donVi != 0) str.Append(DocMot(donVi));

        return str.ToString();
    }

    private static string DocSo(string so)
    {
        so = decimal.Parse(so, CultureInfo.InvariantCulture).ToString("0", CultureInfo.InvariantCulture);
        if (decimal.Parse(so, CultureInfo.InvariantCulture) == 0)
            return " không ";

        var soAm = so.StartsWith('-');
        if (soAm) so = so[1..];

        double ti = 0;
        string nSoText;
        if (so.Length <= 9)
            nSoText = so;
        else
        {
            nSoText = so[^9..];
            ti = double.Parse(so[..^9], CultureInfo.InvariantCulture);
        }

        double trieu = 0, ngan = 0, donvi;
        if (nSoText.Length > 6)
        {
            trieu = double.Parse(nSoText[..^6], CultureInfo.InvariantCulture);
            ngan = double.Parse(nSoText.Substring(nSoText.Length - 6, 3), CultureInfo.InvariantCulture);
            donvi = double.Parse(nSoText[^3..], CultureInfo.InvariantCulture);
        }
        else if (nSoText.Length > 3)
        {
            ngan = double.Parse(nSoText[..^3], CultureInfo.InvariantCulture);
            donvi = double.Parse(nSoText[^3..], CultureInfo.InvariantCulture);
        }
        else
        {
            donvi = double.Parse(nSoText, CultureInfo.InvariantCulture);
        }

        var str = "";
        if (trieu > 0)
        {
            if (ti > 0 && trieu < 10) str += " không trăm linh " + DocBa((int)trieu) + " triệu ";
            else if (ti > 0 && trieu < 100) str += " không trăm " + DocBa((int)trieu) + " triệu ";
            else str += DocBa((int)trieu) + " triệu ";
        }

        if (ngan > 0)
        {
            if ((trieu > 0 && ngan < 10) || (ti > 0 && ngan < 10))
                str += " không trăm linh " + DocBa((int)ngan) + " nghìn ";
            else if ((trieu > 0 && ngan < 100 && ngan >= 10) || (ti > 0 && ngan < 100 && ngan >= 10))
                str += " không trăm " + DocBa((int)ngan) + " nghìn ";
            else
                str += DocBa((int)ngan) + " nghìn ";
        }

        if (donvi > 0)
        {
            if ((ngan > 0 && donvi < 10) || (trieu > 0 && donvi < 10))
                str += " không trăm linh " + DocBa((int)donvi);
            else if ((ngan > 0 && donvi < 100 && donvi >= 10) || (trieu > 0 && donvi < 100 && donvi >= 10))
                str += " không trăm " + DocBa((int)donvi);
            else
                str += DocBa((int)donvi);
        }

        if (ti > 0)
            str = DocSo(ti.ToString(CultureInfo.InvariantCulture)) + " tỉ " + str;

        str = soAm ? " âm " + str.TrimStart() : str;
        return str;
    }
}
