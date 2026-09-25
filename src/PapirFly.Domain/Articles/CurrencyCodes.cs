using System.Collections.Frozen;

namespace PapirFly.Domain.Articles;

/// <summary>Provides the ISO 4217 List One snapshot published by SIX on 2026-09-17.</summary>
public static class CurrencyCodes
{
    // ISO 4217 List One, published 2026-09-17 by SIX (the maintenance agency).
    // Snapshot keeps validation deterministic across platforms and independent of network/OS locale data.
    // https://www.six-group.com/dam/download/financial-information/data-center/iso-currrency/lists/list-one.xml
    private static readonly FrozenSet<string> Codes = """
        AED AFN ALL AMD AOA ARS AUD AWG AZN BAM BBD BDT BHD BIF BMD BND BOB BOV BRL BSD
        BTN BWP BYN BZD CAD CDF CHE CHF CHW CLF CLP CNY COP COU CRC CUP CVE CZK DJF DKK
        DOP DZD EGP ERN ETB EUR FJD FKP GBP GEL GHS GIP GMD GNF GTQ GYD HKD HNL HTG HUF
        IDR ILS INR IQD IRR ISK JMD JOD JPY KES KGS KHR KMF KPW KRW KWD KYD KZT LAK LBP
        LKR LRD LSL LYD MAD MDL MGA MKD MMK MNT MOP MRU MUR MVR MWK MXN MXV MYR MZN NAD
        NGN NIO NOK NPR NZD OMR PAB PEN PGK PHP PKR PLN PYG QAR RON RSD RUB RWF SAR SBD
        SCR SDG SEK SGD SHP SLE SOS SRD SSP STN SVC SYP SZL THB TJS TMT TND TOP TRY TTD
        TWD TZS UAH UGX USD USN UYI UYU UYW UZS VED VES VND VUV WST XAD XAF XAG XAU XBA
        XBB XBC XBD XCD XCG XDR XOF XPD XPF XPT XSU XTS XUA XXX YER ZAR ZMW ZWG
        """.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Checks whether a code is present in the currency snapshot.</summary>
    /// <param name="code">The uppercase alphabetic ISO 4217 code to check.</param>
    /// <returns>True for a listed code; otherwise false. Comparison is ordinal and case-sensitive.</returns>
    public static bool Contains(string code) => Codes.Contains(code);
}
