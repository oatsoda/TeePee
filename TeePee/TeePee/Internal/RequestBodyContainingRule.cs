namespace TeePee.Internal;

internal class RequestBodyContainingRule
{
    public Type RuleType { get; }
    public Func<object, bool> Rule { get; }

    public RequestBodyContainingRule(Type ruleType, Func<object, bool> rule)
    {
        RuleType = ruleType;
        Rule = rule;
    }
};