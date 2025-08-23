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


### 3. Auto Disconnect on Low Health

**Status**: 📋 Planned  
**Complexity**: Medium  
**Dependencies**: AutoPotion system, ObjectManager

**Tasks**:
- [ ] Extend `AutoPotion.cs` with disconnect logic
- [ ] Add health threshold setting for auto-disconnect
- [ ] Implement potion availability check
- [ ] Add safe disconnect method (quit to menu)
- [ ] Add confirmation/confirmation bypass setting
- [ ] Test in both online and offline modes

**Implementation Notes**:
- Reuse existing health monitoring from AutoPotion
- Add potion inventory checking logic
- Use `OnApplicationQuit` or similar for safe disconnect
- Consider adding delay/grace period before disconnect

## 🎯 Medium Priority Features

### 4. Stash Button in Inventory

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

### Item Tooltip Enhancements (Affix Tiers)

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
- [x] Add direct `NetConnection.SendMessage` attempt and client/peer fallbacks
- [ ] Validate effectiveness across scenes and very long idles (30m+)

**Desired Future Improvements**:
- [ ] Protocol-correct keepalive: identify real ping/keepalive opcode/payload and use it instead of generic tiny user message; fall back intelligently.
- [ ] Detection minimization: add jitter to intervals, gate sends to connected/active states only, and adapt/back off on failures to reduce telemetry footprint.

**Implementation Notes**:
- Heartbeat writes prefer FIELD, fallback to PROPERTY; target = NetTime.Now
- Minimal logs: heartbeat write lines and brief status snapshots only
- `ServerConnection` captured; `m_timeoutDeadline` advances as expected

## 🔄 On Hold / Future Features

### 6. NPC Icons on Minimap

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

## 🧪 Testing & Validation

### 9. Testing Framework

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

### 10. Documentation Updates

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
2. **Auto Disconnect** - Extends AutoPotion, safety feature
3. **Stash Button** - High user value, but complex UI integration
4. **Anti-Idle Prevention** - Completed; add minor config and long-run validation
5. **NPC Minimap Icons** - On hold until DMMap system stabilizes

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
