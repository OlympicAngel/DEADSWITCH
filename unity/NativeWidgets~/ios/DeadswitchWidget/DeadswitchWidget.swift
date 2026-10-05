// DEADSWITCH lock-screen and home-screen widget (doc 08 s5): threat level, heat and the next timer from the
// game's leave-time snapshot in the shared App Group (written by Plugins/DsWidget.mm).
import SwiftUI
import WidgetKit

private let group = "group.com.deadswitch.shared"

struct DsEntry: TimelineEntry {
    let date: Date
    let threat: String
    let heat: String
    let next: String
}

struct DsProvider: TimelineProvider {
    func placeholder(in context: Context) -> DsEntry {
        DsEntry(date: Date(), threat: "QUIET", heat: "RUSTBORN COLD 4", next: "NOTHING FORECAST")
    }

    func getSnapshot(in context: Context, completion: @escaping (DsEntry) -> Void) {
        completion(read())
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<DsEntry>) -> Void) {
        completion(Timeline(entries: [read()], policy: .after(Date().addingTimeInterval(15 * 60))))
    }

    private func read() -> DsEntry {
        let d = UserDefaults(suiteName: group)
        return DsEntry(
            date: Date(),
            threat: d?.string(forKey: "threat") ?? "OPEN THE TERMINAL",
            heat: d?.string(forKey: "heat") ?? "",
            next: d?.string(forKey: "next") ?? "")
    }
}

struct DsWidgetView: View {
    let entry: DsEntry

    var body: some View {
        VStack(alignment: .leading, spacing: 2) {
            Text(entry.threat).font(.system(size: 14, weight: .bold)).foregroundColor(Color(red: 0.91, green: 0.71, blue: 0.36))
            Text(entry.heat).font(.system(size: 12, design: .monospaced))
            Text(entry.next).font(.system(size: 12, design: .monospaced)).foregroundColor(Color(red: 0.66, green: 0.84, blue: 0.54))
        }
        .containerBackground(for: .widget) { Color(red: 0.07, green: 0.08, blue: 0.07) }
    }
}

@main
struct DeadswitchWidget: Widget {
    var body: some WidgetConfiguration {
        StaticConfiguration(kind: "DeadswitchWidget", provider: DsProvider()) { entry in
            DsWidgetView(entry: entry)
        }
        .configurationDisplayName("DEADSWITCH")
        .description("Threat, heat and the next timer from the core.")
        .supportedFamilies([.systemSmall, .accessoryRectangular])
    }
}
