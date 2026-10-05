// DEADSWITCH widget bridge (doc 08 s5): the game writes its leave-time snapshot to the shared App Group that the
// WidgetKit extension reads. Enabled with the DS_IOS_WIDGET scripting define (WidgetBridge.cs).
#import <Foundation/Foundation.h>

static NSString* const DsGroup = @"group.com.deadswitch.shared";

extern "C" void _DsWidgetWrite(const char* threat, const char* heat, const char* next)
{
    NSUserDefaults* d = [[NSUserDefaults alloc] initWithSuiteName:DsGroup];
    [d setObject:[NSString stringWithUTF8String:threat ? threat : ""] forKey:@"threat"];
    [d setObject:[NSString stringWithUTF8String:heat ? heat : ""] forKey:@"heat"];
    [d setObject:[NSString stringWithUTF8String:next ? next : ""] forKey:@"next"];
    [d setObject:[NSDate date] forKey:@"updated"];
}
