﻿using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using GitHub.Services;
using GitHub.VisualStudio;
using GitHub.VisualStudio.Base;
using NUnit.Framework;
using NSubstitute;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;

public class VSGitExtTests
{
    public class TheConstructor
    {
        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void GetServiceIGitExt_WhenSccProviderContextIsActive(bool isActive, int expectCalls)
        {
            var context = CreateVSUIContext(isActive);
            var sp = CreateGitHubServiceProvider(context);

            var target = new VSGitExt(sp);

            sp.Received(expectCalls).GetService<IGitExt>();
        }

        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void GetServiceIGitExt_WhenUIContextChanged(bool activated, int expectCalls)
        {
            var context = CreateVSUIContext(false);
            var sp = CreateGitHubServiceProvider(context);

            var target = new VSGitExt(sp);
            var eventArgs = Substitute.For<IVSUIContextChangedEventArgs>();
            eventArgs.Activated.Returns(activated);
            context.UIContextChanged += Raise.Event<EventHandler<IVSUIContextChangedEventArgs>>(context, eventArgs);

            sp.Received(expectCalls).GetService<IGitExt>();
        }
    }

    public class TheActiveRepositoriesChangedEvent
    {
        [Test]
        public void GitExtPropertyChangedEvent_ActiveRepositoriesChangedIsFired()
        {
            var context = CreateVSUIContext(true);
            var gitExt = CreateGitExt();
            var sp = CreateGitHubServiceProvider(context, gitExt);
            var target = new VSGitExt(sp);

            bool wasFired = false;
            target.ActiveRepositoriesChanged += () => wasFired = true;
            var eventArgs = new PropertyChangedEventArgs(nameof(gitExt.ActiveRepositories));
            gitExt.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(gitExt, eventArgs);

            Assert.That(wasFired, Is.True);
        }

        [Test]
        public void WhenUIContextChanged_ActiveRepositoriesChangedIsFired()
        {
            var context = CreateVSUIContext(false);
            var gitExt = CreateGitExt();
            var sp = CreateGitHubServiceProvider(context, gitExt);
            var target = new VSGitExt(sp);

            bool wasFired = false;
            target.ActiveRepositoriesChanged += () => wasFired = true;

            var eventArgs = Substitute.For<IVSUIContextChangedEventArgs>();
            eventArgs.Activated.Returns(true);
            context.UIContextChanged += Raise.Event<EventHandler<IVSUIContextChangedEventArgs>>(context, eventArgs);

            Assert.That(wasFired, Is.True);
        }
    }

    public class TheActiveRepositoriesProperty
    {
        [Test]
        public void SccProviderContextNotActive_IsNull()
        {
            var context = CreateVSUIContext(false);
            var sp = CreateGitHubServiceProvider(context);

            var target = new VSGitExt(sp);

            Assert.That(target.ActiveRepositories, Is.Null);
        }

        [Test]
        public void SccProviderContextIsActive_DefaultEmptyListFromGitExt()
        {
            var context = CreateVSUIContext(true);
            var gitExt = CreateGitExt(new string[0]);
            var sp = CreateGitHubServiceProvider(context, gitExt);
            var target = new VSGitExt(sp);

            var activeRepositories = target.ActiveRepositories;

            Assert.That(activeRepositories.Count(), Is.EqualTo(0));
        }

        // TODO: We can't currently test returning a non-empty list because it constructs a live LocalRepositoryModel object.
        // Move could move the responsibility for constructing a LocalRepositoryModel object outside of VSGitExt.
    }

    static IVSUIContext CreateVSUIContext(bool isActive)
    {
        var context = Substitute.For<IVSUIContext>();
        context.IsActive.Returns(isActive);
        return context;
    }

    static IGitExt CreateGitExt(IList<string> repositoryPaths = null)
    {
        repositoryPaths = repositoryPaths ?? new string[0];
        var gitExt = Substitute.For<IGitExt>();
        var repoList = CreateActiveRepositories(new string[0]);
        gitExt.ActiveRepositories.Returns(repoList);
        return gitExt;
    }

    static IReadOnlyList<IGitRepositoryInfo> CreateActiveRepositories(IList<string> repositoryPaths)
    {
        var repositories = new List<IGitRepositoryInfo>();
        foreach (var repositoryPath in repositoryPaths)
        {
            var repoInfo = Substitute.For<IGitRepositoryInfo>();
            repoInfo.RepositoryPath.Returns(repositoryPath);
            repositories.Add(repoInfo);
        }

        return repositories.AsReadOnly();
    }

    static IGitHubServiceProvider CreateGitHubServiceProvider(IVSUIContext context, IGitExt gitExt = null)
    {
        var contextFactory = Substitute.For<IVSUIContextFactory>();
        gitExt = gitExt ?? Substitute.For<IGitExt>();
        var contextGuid = new Guid(Guids.GitSccProviderId);
        contextFactory.GetUIContext(contextGuid).Returns(context);
        var sp = Substitute.For<IGitHubServiceProvider>();
        sp.GetService<IVSUIContextFactory>().Returns(contextFactory);
        sp.GetService<IGitExt>().Returns(gitExt);
        return sp;
    }
}