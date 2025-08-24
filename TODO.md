# Last Epoch Mod - TODO List

## 🚀 High Priority Features

### 1. Shrine Detection System

**Status**: 🔄 In Progress  
**Complexity**: Medium  
**Dependencies**: ESP system, ObjectManager

**Tasks**:
- [ ] Extend `ObjectManager` to detect shrine entities
- [ ] Add shrine detection logic similar to special enemies (Magic/Rare)
- [ ] Implement shrine classification system in `Settings.cs`
- [ ] Add shrine drawing/ESP functionality
- [ ] Test shrine detection accuracy across different zones

**Implementation Notes**:
- Leverage existing `ShouldDrawShrine()` method in Settings
- Use similar approach to `DisplayActorClass` for shrine types
- Consider shrine states (active/inactive, buffed/unbuffed)

### 2. Auto Disconnect on Low Health

**Status**: ✅ Deployed (UI-based quit; optional gating by potions)  
**Complexity**: Medium  
**Dependencies**: AutoPotion component cache, ObjectManager, AntiIdleSystem

**What exists now**:
- [x] `Settings`: flags and thresholds (`useAutoDisconnect`, `autoDisconnectHealthPercent`, `autoDisconnectCooldownSeconds`, `autoDisconnectOnlyWhenNoPotions`)
- [x] `Cheats/AutoDisconnect.cs`: reads `PlayerHealth.getHealthPercent()`, cooldown debounce, Reaper-form guard, lazy+hooked `UIBase` capture, calls `UIBase.ExitToLogin()`
- [x] Wired into `Mod.OnUpdate`, cache cleared on scene init with lazy re-discovery
- [x] Menu wiring next to AutoPotion controls

**Tasks**:
- [x] Implement safe quit-to-menu invocation (native `UIBase.ExitToLogin()`)
- [x] Add toggle: Only disconnect when out of potions (uses `LocalPlayer.healthPotion.currentCharges`)
- [x] Finalize concise logging
- [ ] Add suppression window integration (reuse AntiIdle scene/network suppression)
- [ ] Add robust state guards for loading screens and death
- [ ] Test in both online/offline modes, across scenes, with low FPS

**Implementation Notes**:
- `UIBase` instance obtained via `UIBase.Awake` hook and lazy `FindObjectOfType`/`Resources.FindObjectsOfTypeAll`
- All calls on main thread; action idempotent under debounce window

## 🎯 Medium Priority Features

### 3. Stash Button in Inventory

**Status**: 📋 Planned  
**Complexity**: High  
**Dependencies**: UI patching system, inventory system

**Tasks**:
- [ ] Research inventory UI structure and components
- [ ] Implement UI element injection for stash button
- [ ] Add button positioning and styling
- [ ] Implement stash opening functionality
- [ ] Add hotkey support for stash button
- [ ] Test button functionality across different inventory states

**Implementation Notes**:
- Study KG's mod implementation for reference
- Use Harmony patches for UI element injection
- Consider button placement (bottom of inventory)
- Add visual feedback for button interactions

### 4. Item Tooltip Enhancements (Affix Tiers)

**Status**: 📋 Planned  
**Complexity**: Medium-High  
**Dependencies**: Item tooltip UI, item data parsing

**Tasks**:
- [ ] Hook into item tooltip creation/update to obtain the current tooltip object
- [ ] Parse item affixes and determine tier values for each affix
- [ ] Extend tooltip UI to display per-affix tier (e.g., T1–T7)
- [ ] Add settings to enable/disable tier display and style
- [ ] Validate for different item rarities and crafted/exalted tiers
- [ ] Ensure performance is acceptable when hovering rapidly over items

**Implementation Notes**:
- Prefer postfix patches on tooltip build methods; cache reflection where possible
- Avoid allocations in per-frame updates; update UI only on tooltip refresh
- Consider color-coding tiers and handling hybrid/implicit affixes

### 5. Anti-Idle Kick Prevention

**Status**: ✅ Deployed (synthetic keepalive with jitter/state gating); further validation ongoing  
**Complexity**: Low-Medium  
**Dependencies**: Networking patches

**Tasks**:
- [x] Research idle detection mechanisms (NetMultiClient.ConnectionStatus)
- [x] Create AntiIdleSystem with targeted logging
- [x] Add networking patches for connection monitoring
- [x] Implement periodic anti-idle action system
- [x] Fix namespaces and apply Harmony bootstrap
- [x] Discover heartbeat timer threshold (~100s) and implement reset
- [x] Use NetTime.Now and recursive FIELD write for `m_lastHeartbeat`
- [x] Capture `ServerConnection` robustly (property/collections)
- [x] Tone down logging (remove per-frame ConnectionStatus pulses; minimal snapshots)
- [x] Add configurable heartbeat frequency (Settings)
- [x] Add synthetic keepalive (ReliableUnordered), with jittered intervals and connected-state gating
- [x] Suppression gates: pause synthetic keepalive on user activity, scene change, and outbound network traffic
- [ ] Validate effectiveness across scenes and very long idles (30m+)

**What exists now**:
- Jittered send interval with ±2s randomness to avoid signatures
- Connected-state gating; one-shot snapshots on status change only
- Suppression windows: input activity, scene change, and network-send events
- Quiet heartbeat reset attempts; minimal logs toggled via internal flags

**Desired Future Improvements**:
- [ ] Protocol-correct keepalive: identify real ping/keepalive opcode/payload and use it instead of generic tiny user message; fall back intelligently.
- [ ] Backoff tuning: adaptive jitter/backoff on failures; consolidate suppression configuration

**Implementation Notes**:
- Heartbeat writes prefer FIELD, fallback to PROPERTY; target = NetTime.Now
- Minimal logs: heartbeat write lines and brief status snapshots only
- `ServerConnection` captured; `m_timeoutDeadline` advances as expected

### 6. Minimap Enemy Circles (Stopgap)

**Status**: ✅ Deployed (basic overlay; approximations)  
**Complexity**: Medium  
**Dependencies**: ActorManager, UI hierarchy (`DMMap Canvas/Icons`)

**What exists now**:
- [x] Basic minimap enemy circles parented under `DMMap Canvas/Icons`
- [x] Rarity-colored sprites (white/blue/yellow/red) with pooled textures
- [x] World→minimap mapping with basis rotation, map rotation, axis flips
- [x] Auto-scale by `Icons` rect and `minimapWorldRadiusMeters`; adjustable via `minimapScaleFactor`
- [x] Fullscreen map suppression via explicit sentinel path

**Limitations**:
- Uses heuristic scaling/rotation; not using native DMMap conversion or zoom
- Overlay may drift with unusual map modes/zoom; manual tuning required
- Manual cleanup; relies on our lifecycle instead of DMMap's

**Next steps**:
- [ ] Replace overlay with native DMMap icon API (preferred)
- [ ] Read DMMap zoom/rotation directly; remove basis/flip hacks
- [ ] Bind to DMap lifecycle (create/destroy) so icons clean up naturally
- [ ] Remove overlay code once DMMap path is stable

## 🔄 On Hold / Future Features

### 7. NPC Icons on Minimap

**Status**: ⏸️ On Hold  
**Complexity**: High  
**Dependencies**: DMMap subsystem (needs update)

**Tasks**:
- [ ] Wait for DMMap subsystem to stabilize after game update
- [ ] Research new minimap icon system
- [ ] Implement NPC icon placement logic
- [ ] Add icon customization options
- [ ] Test performance with multiple NPC icons
- [ ] Add filtering options for different NPC types

**Implementation Notes**:
- Previous DMMap implementation was partially working
- Need to adapt to new game update changes
- Consider performance impact of multiple minimap icons
- May need to wait for game update to settle
- Note: A stopgap overlay renderer (enemy circles) is currently deployed and will be replaced by the native DMMap icon path when feasible

## 🛠️ Technical Improvements

### 7. Code Quality & Performance

**Status**: 📋 Ongoing  
**Complexity**: Low-Medium

**Tasks**:
- [ ] Optimize ESP rendering performance
- [ ] Reduce GC pressure in update loops
- [ ] Add comprehensive error handling
- [ ] Implement feature flag system for better modularity
- [ ] Add performance monitoring/logging
- [ ] Refactor duplicate code in ESP systems

### 8. Configuration & Settings

**Status**: 📋 Ongoing  
**Complexity**: Low

**Tasks**:
- [ ] Add MelonPreferences integration for persistent settings
- [ ] Create settings UI for new features
- [ ] Add hotkey configuration system
- [ ] Implement settings validation
- [ ] Add settings import/export functionality
- [ ] Create settings documentation

### 9. Auto Potion Enhancements (Remaining Charges & Logging)

**Status**: ✅ Deployed  
**Complexity**: Low

**What exists now**:
- [x] Directly read `LocalPlayer.healthPotion.currentCharges` / `maxCharges` (Il2Cpp.HealthPotionCharges)
- [x] Log when out of potions; include estimated remaining on use
- [x] Expose `TryGetRemainingPotions()` for other systems (e.g., AutoDisconnect)

**Next steps**:
- [ ] Optional overlay/debug display of remaining potions
- [ ] Consider exposing max charges in Menu and guard for loading states

## 🧪 Testing & Validation

### 10. Testing Framework

**Status**: 📋 Planned  
**Complexity**: Medium

**Tasks**:
- [ ] Create unit tests for core systems
- [ ] Add integration tests for feature interactions
- [ ] Implement automated testing for UI elements
- [ ] Add performance benchmarking
- [ ] Create test scenarios for edge cases
- [ ] Add regression testing for game updates

## 📚 Documentation

### 11. Documentation Updates

**Status**: 📋 Ongoing  
**Complexity**: Low

**Tasks**:
- [ ] Update README with new features
- [ ] Create user guide for new functionality
- [ ] Document API changes and breaking changes
- [ ] Add troubleshooting guide
- [ ] Create developer documentation
- [ ] Add code comments for complex systems

---

## 🎯 Implementation Priority Order

1. **Shrine Detection** - Extends existing ESP system, moderate complexity
2. **Auto Disconnect** - Implemented and optionally gated by potions; add suppress/state guards next
3. **Stash Button** - High user value, but complex UI integration
4. **Anti-Idle Prevention** - Completed; add minor config and long-run validation
5. **NPC Minimap Icons** - On hold until DMMap system stabilizes (stopgap overlay deployed)
6. **Potion Count Display** - Add overlay/debug display of remaining potions

## 🔧 Technical Considerations

- **Performance**: Monitor ESP rendering impact, especially with new features
- **Compatibility**: Test all features in both online and offline modes
- **Safety**: Ensure auto-disconnect doesn't cause data loss
- **User Experience**: Add appropriate toggles and settings for all features
- **Maintainability**: Keep code modular and well-documented

## 📝 Notes

- All new features should include appropriate error handling
- Consider adding feature flags for easy enabling/disabling
- Test thoroughly in different game states and scenarios
- Maintain backward compatibility where possible
- Document any breaking changes or new dependencies
