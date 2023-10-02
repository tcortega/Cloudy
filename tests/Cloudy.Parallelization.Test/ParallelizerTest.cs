using System.Diagnostics;
using Cloudy.Parallelization.Models;
using FluentAssertions;

namespace Cloudy.Parallelization.Test;

public class ParallelizerTests
    {
        private readonly Func<int, CancellationToken, Task<bool>> _parityCheck 
            = (number, _) => Task.FromResult(number % 2 == 0);

        private readonly Func<int, CancellationToken, Task<bool>> _longTask 
            = async (_, _) => { await Task.Delay(100); return true; };

        private const ParallelizerType Type = ParallelizerType.TaskBased;

        private int _progressCount;
        private bool _completedFlag;
        private Exception? _lastException;
        private bool _lastResult;
        
        private void OnProgress(object? sender, float value) => _progressCount++;
        private void OnResult(object? sender, ResultDetails<int, bool> value) => _lastResult = value.Result;
        private void OnCompleted(object? sender, EventArgs e) => _completedFlag = true;
        private void OnException(object? sender, Exception? ex) => _lastException = ex;

        private Parallelizer<int, bool> CreateParallelizer(Func<int, CancellationToken, Task<bool>> workFunction, int count = 100, int parallelism = 1)
        {
            var parallelizer = ParallelizerFactory<int, bool>.Create(
                type: Type,
                workItems: Enumerable.Range(1, count),
                workFunction: workFunction,
                degreeOfParallelism: parallelism,
                totalAmount: count,
                skip: 0);

            _progressCount = 0;
            _completedFlag = false;
            _lastException = null;

            parallelizer.ProgressChanged += OnProgress;
            parallelizer.NewResult += OnResult;
            parallelizer.Completed += OnCompleted;
            parallelizer.Error += OnException;

            return parallelizer;
        }

        [Fact]
        public async Task Run_QuickTasks_CompleteAndCall()
        {
            var parallelizer = CreateParallelizer(_parityCheck);

            await parallelizer.Start();
            await parallelizer.WaitCompletion();

            _progressCount.Should().Be(100);
            _completedFlag.Should().BeTrue();
            _lastException.Should().BeNull();
            _lastResult.Should().BeTrue();
        }

        [Fact]
        public async Task Run_QuickTasks_StopwatchStops()
        {
            var parallelizer = CreateParallelizer(_parityCheck);

            await parallelizer.Start();
            await parallelizer.WaitCompletion();

            var elapsed = parallelizer.Elapsed;
            await Task.Delay(1000);
            elapsed.Should().Be(parallelizer.Elapsed);
        }

        [Fact]
        public async Task Run_LongTasks_StopBeforeCompletion()
        {
            var parallelizer = CreateParallelizer(_longTask, 1000, 10);

            await parallelizer.Start();
            await Task.Delay(250);

            await parallelizer.Stop();

            _progressCount.Should().BeInRange(10, 50);
            _completedFlag.Should().BeTrue();
            _lastException.Should().BeNull();
        }

        [Fact]
        public async Task Run_LongTasks_AbortBeforeCompletion()
        {
            var parallelizer = CreateParallelizer(_longTask, 1000, 10);

            await parallelizer.Start();
            await Task.Delay(250);

            await parallelizer.Abort();
            await parallelizer.WaitCompletion();

            _progressCount.Should().BeInRange(10, 50);
            _completedFlag.Should().BeTrue();
            _lastException.Should().BeNull();
        }

        [Fact]
        public async Task Run_IncreaseConcurrentThreads_CompleteFaster()
        {
            var parallelizer = CreateParallelizer(_longTask, 10);
            
            var stopwatch = Stopwatch.StartNew();

            await parallelizer.Start();
            await Task.Delay(250);
            await parallelizer.ChangeDegreeOfParallelism(4);
            await parallelizer.WaitCompletion();

            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(800);
        }

        [Fact]
        public async Task Run_DecreaseConcurrentThreads_CompleteSlower()
        {
            var parallelizer = CreateParallelizer(_longTask, 12, 3);

            var stopwatch = Stopwatch.StartNew();

            await parallelizer.Start();
            await Task.Delay(150);
            await parallelizer.ChangeDegreeOfParallelism(1);
            await parallelizer.WaitCompletion();

            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(600);
        }

        [Fact]
        public async Task Run_PauseAndResume_CompleteAll()
        {
            var count = 10;
            var parallelizer = CreateParallelizer(_longTask, count);

            await parallelizer.Start();
            await Task.Delay(150);
            await parallelizer.Pause();

            var progress = _progressCount;
            await Task.Delay(1000);
            progress.Should().Be(_progressCount);

            await parallelizer.Resume();
            await parallelizer.WaitCompletion();

            _progressCount.Should().Be(count);
            _completedFlag.Should().BeTrue();
            _lastException.Should().BeNull();
        }

        [Fact]
        public async Task Run_Pause_StopwatchStops()
        {
            var count = 10;
            var parallelizer = CreateParallelizer(_longTask, count);

            await parallelizer.Start();
            await Task.Delay(150);
            await parallelizer.Pause();

            var elapsed = parallelizer.Elapsed;
            await Task.Delay(1000);
            elapsed.Should().Be(parallelizer.Elapsed);

            await parallelizer.Abort();
        }
    }