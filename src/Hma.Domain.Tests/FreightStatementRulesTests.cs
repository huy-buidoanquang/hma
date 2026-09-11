using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class FreightStatementRulesTests
{
    [Fact]
    public void Generate_only_allows_new_or_draft_statement()
    {
        FreightStatementRules.EnsureCanGenerate(null);
        FreightStatementRules.EnsureCanGenerate(new FreightStatement());

        Assert.Throws<InvalidOperationException>(() => FreightStatementRules.EnsureCanGenerate(
            new FreightStatement { Status = FinancialDocumentStatus.Submitted }));
    }

    [Fact]
    public void Submit_requires_non_empty_draft_and_authenticated_user()
    {
        var statement = Statement(FinancialDocumentStatus.Draft, withLine: true);

        FreightStatementRules.EnsureCanSubmit(statement, 1);
        Assert.Throws<InvalidOperationException>(() => FreightStatementRules.EnsureCanSubmit(statement, null));
        Assert.Throws<InvalidOperationException>(() => FreightStatementRules.EnsureCanSubmit(
            Statement(FinancialDocumentStatus.Draft, withLine: false), 1));
        Assert.Throws<InvalidOperationException>(() => FreightStatementRules.EnsureCanSubmit(
            Statement(FinancialDocumentStatus.Submitted, withLine: true), 1));
    }

    [Fact]
    public void Finalize_enforces_maker_checker()
    {
        var statement = Statement(FinancialDocumentStatus.Submitted, withLine: true);
        statement.SubmittedByUserId = 10;

        Assert.Throws<InvalidOperationException>(() =>
            FreightStatementRules.EnsureCanFinalize(statement, 10));
        FreightStatementRules.EnsureCanFinalize(statement, 11);
    }

    [Fact]
    public void Void_requires_manager_finalized_statement_and_reason()
    {
        var statement = Statement(FinancialDocumentStatus.Finalized, withLine: true);

        Assert.Throws<InvalidOperationException>(() =>
            FreightStatementRules.EnsureCanVoid(statement, false, "Sai kỳ"));
        Assert.Throws<InvalidOperationException>(() =>
            FreightStatementRules.EnsureCanVoid(statement, true, " "));
        FreightStatementRules.EnsureCanVoid(statement, true, "Sai kỳ");
    }

    private static FreightStatement Statement(FinancialDocumentStatus status, bool withLine)
    {
        var statement = new FreightStatement { Status = status };
        if (withLine)
            statement.Lines.Add(new FreightStatementLine { DispatchOrderId = 1 });
        return statement;
    }
}
