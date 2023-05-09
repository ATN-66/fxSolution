const double n01 = 1.21080;
const double n02 = 1.20911;
const double n03 = 0.09172;
const double n04 = 0.04153;
const double n05 = 1.05124;
const double n06 = 1.20915;
const double n07 = 0.09126;
const double n08 = 0.09137;
const double n09 = 1.09188;
const double n10 = 1.09129;
const double n11 = 0.091371234;
const double n12 = 1.091807865;
const double n13 = 1.091215664532323;

var r01 = Ask_EUR_USD_GBP_Ceiling(n01);
var r02 = Ask_EUR_USD_GBP_Ceiling(n02);
var r03 = Ask_EUR_USD_GBP_Ceiling(n03);
var r04 = Ask_EUR_USD_GBP_Ceiling(n04);
var r05 = Ask_EUR_USD_GBP_Ceiling(n05);
var r06 = Ask_EUR_USD_GBP_Ceiling(n06);
var r07 = Ask_EUR_USD_GBP_Ceiling(n07);
var r08 = Ask_EUR_USD_GBP_Ceiling(n08);
var r09 = Ask_EUR_USD_GBP_Ceiling(n09);
var r10 = Ask_EUR_USD_GBP_Ceiling(n10);
var r11 = Ask_EUR_USD_GBP_Ceiling(n11);
var r12 = Ask_EUR_USD_GBP_Ceiling(n12);
var r13 = Ask_EUR_USD_GBP_Ceiling(n13);

Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n01} -> {r01}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n02} -> {r02}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n03} -> {r03}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n04} -> {r04}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n05} -> {r05}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n06} -> {r06}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n07} -> {r07}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n08} -> {r08}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n09} -> {r09}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n10} -> {r10}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n11} -> {r11}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n12} -> {r12}");
Console.WriteLine($"Ask_EUR_USD_GBP_Ceiling: {n13} -> {r13}");

// output:
//Ask_EUR_USD_GBP_Ceiling: 1.21080-> 1.2108
//Ask_EUR_USD_GBP_Ceiling: 1.20911-> 1.2091
//Ask_EUR_USD_GBP_Ceiling: 0.09172-> 0.0917
//Ask_EUR_USD_GBP_Ceiling: 0.04153-> 0.0415
//Ask_EUR_USD_GBP_Ceiling: 1.05124-> 1.0513
//Ask_EUR_USD_GBP_Ceiling: 1.20915-> 1.2092
//Ask_EUR_USD_GBP_Ceiling: 0.09126-> 0.0913
//Ask_EUR_USD_GBP_Ceiling: 0.09137-> 0.0914
//Ask_EUR_USD_GBP_Ceiling: 1.09188-> 1.0919
//Ask_EUR_USD_GBP_Ceiling: 1.09129-> 1.0913
//Ask_EUR_USD_GBP_Ceiling: 0.091371234-> 0.0914
//Ask_EUR_USD_GBP_Ceiling: 1.091807865-> 1.0918
//Ask_EUR_USD_GBP_Ceiling: 1.091215664532323-> 1.0912












Console.ReadKey();

static double Ask_EUR_USD_GBP_Ceiling(double value)
{
    const int multiplier = 100_000;
    var multipliedValue = (int)(value * multiplier);
    var (Quotient, Remainder) = Math.DivRem(multipliedValue, 10);
    if (Remainder > 3) Quotient += 1;
    var result = Quotient / (multiplier / 10d);
    return result;
}


