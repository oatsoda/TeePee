namespace TeePee;

internal record RequestBodyContainingRule(Type RuleType, Func<object, bool> Rule);