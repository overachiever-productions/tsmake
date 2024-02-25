using NUnit.Framework.Internal;
using System.Collections.Generic;

namespace tsmake.tests.unit_tests.directive_tests;

[TestFixture]
public class IncludeDirectoryDirectiveTests
{
    // TODO: current tests are 'happy-path' in nature. Need to add some tests for error cases - i.e., tsmake needs to help users into the 'pit of success' - via error messages/etc. 

    [Test]
    public void Simple_Directory_Directive_Extracts_Directory_Name()
    {
        var line = new Line(12, "-- ## DIRECTORY: Common ", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.Path, Is.EqualTo("Common"));
    }

    [Test]
    public void Simple_Directory_Directive_Identifies_Directory_Path_Type()
    {
        var line = new Line(12, "-- ## DIRECTORY: Common ", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.PathType, Is.EqualTo(PathType.Relative));
    }

    [Test]
    public void Directory_Directive_With_Explicit_Path_Extracts_Directory_Name()
    {
        var line = new Line(12, "-- ## DIRECTORY: PATH: Utilities ", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.Path, Is.EqualTo("Utilities"));
    }

    [Test]
    public void Directory_Directive_Without_OrderBy_Orders_Alphabetically_By_Default()
    {
        var line = new Line(12, "-- ## DIRECTORY: PATH: Utilities ", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.OrderBy, Is.EqualTo(OrderBy.Alphabetical));
    }

    [Test]
    public void Directory_Directive_Sets_Explicit_OrderBy()
    {
        var line = new Line(12, "-- ## DIRECTORY: PATH: Utilities ORDERBY: CreateDate DESC", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.OrderBy, Is.EqualTo(OrderBy.CreateDate));
    }

    [Test]
    public void Directory_Directive_Sets_Explicit_Descending_Direction()
    {
        var line = new Line(12, "-- ## DIRECTORY: PATH: Utilities ORDERBY: CreateDate DESC", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.OrderBy, Is.EqualTo(OrderBy.CreateDate));
        Assert.That(sut.Direction, Is.EqualTo(Direction.Descending));
    }

    [Test]
    public void Directory_Directive_With_Single_Exclusion_Captures_Exclusion()
    {
        var line = new Line(12, "-- ## DIRECTORY: PATH: Utilities ORDERBY: CreateDate DESC EXCLUDE: %~~%", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.Exclusions.Count, Is.EqualTo(1));
        Assert.That(sut.Exclusions.First(), Is.EqualTo("%~~%"));
    }

    [Test]
    public void Directory_Directive_With_Multiple_Exclusions_Captures_Exclusions()
    {
        var line = new Line(12, "-- ## DIRECTORY: PATH: Utilities ORDERBY: CreateDate DESC EXCLUDE: %~~%, %some_file.sql", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.Exclusions.Count, Is.EqualTo(2));
        Assert.That(sut.Exclusions[0], Is.EqualTo("%~~%"));
        Assert.That(sut.Exclusions[1], Is.EqualTo("%some_file.sql"));
    }

    [Test]
    public void Directory_Directive_With_Priorities_Captures_Priorities_In_Order()
    {
        var line = new Line(12, "-- ## DIRECTORY: PATH: Utilities ORDERBY: CreateDate DESC EXCLUDE: %~~%, %some_file.sql PRIORITIES: %priority_one%, priority_two% ; %second_most_un_priority%, most_un_prioritized", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.Priorities.Count, Is.EqualTo(2));
        Assert.That(sut.Priorities[0], Is.EqualTo("%priority_one%"));
        Assert.That(sut.Priorities[1], Is.EqualTo("priority_two%"));
    }

    [Test]
    public void Directory_Directive_With_Empty_Priorities_Does_Not_Cause_Problems()
    {
        var line = new Line(12, "-- ## DIRECTORY: PATH: Utilities ORDERBY: CreateDate DESC EXCLUDE: %~~%, %some_file.sql PRIORITIES: ;", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.Priorities.Count, Is.EqualTo(0));
    }

    [Test]
    public void Directory_Directive_With_UnPriorities_Captures_Unpriorities_In_Order()
    {
        var line = new Line(12, "-- ## DIRECTORY: PATH: Utilities ORDERBY: CreateDate DESC EXCLUDE: %~~%, %some_file.sql PRIORITIES: %priority_one%, priority_two% ; %second_most_un_priority%, most_un_prioritized", "build.sql");

        var sut = (IncludeDirectoryDirective)line.Directive;

        Assert.True(sut.IsValid);
        Assert.That(sut.UnPriorities.Count, Is.EqualTo(2));
        Assert.That(sut.UnPriorities[0], Is.EqualTo("%second_most_un_priority%"));
        Assert.That(sut.UnPriorities[1], Is.EqualTo("most_un_prioritized"));
    }
}