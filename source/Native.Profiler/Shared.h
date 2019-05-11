#pragma once

#include <array>
#include <atomic>
#include "cor.h"
#include "corprof.h"

static const size_t MAX_MONITORED_ALLOCATIONS = 10;

struct Allocation {
    ObjectID objectId;
    SIZE_T objectSize;
};

struct ProfilerThreadState {
    unsigned long gcCountWhenStartedMonitoring;
	int nextAllocationIndex;
    std::array<Allocation, MAX_MONITORED_ALLOCATIONS> allocations;
    std::array<std::unique_ptr<BYTE[]>, MAX_MONITORED_ALLOCATIONS> gcSafeCopiesOfAllocations;
};

extern thread_local bool threadMonitoringActive;
extern thread_local std::unique_ptr<ProfilerThreadState> threadState;

extern std::atomic_ulong gcCount;