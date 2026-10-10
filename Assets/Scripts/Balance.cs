// One knob for how big the numbers look. Every health pool and every damage source is designed in small
// single digits, then multiplied by this, so the floating damage numbers read big (60, 250...) while the
// fights play out exactly as before: same hits to kill, same resistance. Distances, rates, money, rewards
// and costs are not scaled.
public static class Balance
{
    public const float Scale = 10f;
}
