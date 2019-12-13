//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
//
// Authors:
//   Aaron Bockover <abock@microsoft.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// % clang++ -o query-vsmac -ObjC -fcxx-modules -fmodules -Werror -Wall -Wpedantic query-vsmac.mm

@import AppKit;
@import CoreServices;

#ifdef __cplusplus
#define VS_EXTERN extern "C"
#else
#define VS_EXTERN extern
#endif

VS_EXTERN NSRunningApplication **
VSQueryRunningInstances()
{
    NSArray<NSRunningApplication *> *allApps = NSWorkspace.sharedWorkspace.runningApplications;
    if (!allApps)
        return nil;

    NSRunningApplication **instances = (NSRunningApplication **)calloc(
        [allApps count] + 1,
        sizeof(NSRunningApplication *));
    if (!instances)
        goto ret;

    int i = 0;

    for (NSRunningApplication *runningApp in NSWorkspace.sharedWorkspace.runningApplications) {
        if ([runningApp.bundleIdentifier isEqual: @"com.microsoft.visual-studio"] ||
            [runningApp.bundleIdentifier isEqual: @"com.xamarin.monodevelop"])
            instances[i++] = [runningApp retain];
    }

ret:
    [allApps release];
    return instances;
}

VS_EXTERN void
VSFreeRunningInstances(NSRunningApplication **instances)
{
    if (!instances)
        return;

    for (int i = 0; instances && instances[i]; i++)
        [instances[i] release];

    free(instances);
}

static char *
NSStringToUTF8Dup(NSString *str)
{
    if (str) {
        const char *utf8str = [str UTF8String];
        if (utf8str)
            return strdup(utf8str);
    }

    return NULL;
}

VS_EXTERN char *
VSQueryInstanceCurrentSelectedSolutionPath(NSRunningApplication *runningApplication)
{
    NSAppleEventDescriptor *targetDescriptor = [NSAppleEventDescriptor
        descriptorWithProcessIdentifier: runningApplication.processIdentifier];

    NSAppleEventDescriptor* appleEvent = [NSAppleEventDescriptor
        appleEventWithEventClass: 1448302419
        eventID: 1129534288
        targetDescriptor: targetDescriptor
        returnID: kAutoGenerateReturnID
        transactionID: kAnyTransactionID];

    AEDesc aeReply = { 0, };

    OSErr sendResult = AESendMessage(
        [appleEvent aeDesc],
        &aeReply,
        kAEWaitReply | kAENeverInteract,
        kAEDefaultTimeout);

    [targetDescriptor release];
    [appleEvent release];

    if (sendResult != noErr) {
        return NULL;
    }

    NSAppleEventDescriptor *reply = [[NSAppleEventDescriptor alloc] initWithAEDescNoCopy: &aeReply];
    NSString *path = [[reply descriptorForKeyword: keyDirectObject] stringValue];
    [reply release];

    return NSStringToUTF8Dup(path);
}

VS_EXTERN pid_t
VSGetInstanceProcessIdentifier(NSRunningApplication *instance)
{
    if (!instance)
        return -1;

    return instance.processIdentifier;
}

VS_EXTERN char *
VSGetInstanceBundlePath(NSRunningApplication *instance)
{
    if (!instance || !instance.bundleURL)
        return NULL;

    return NSStringToUTF8Dup(instance.bundleURL.path);
}

VS_EXTERN char *
VSGetInstanceExecutablePath(NSRunningApplication *instance)
{
    if (!instance || !instance.executableURL)
        return NULL;

    return NSStringToUTF8Dup(instance.executableURL.path);
}

VS_EXTERN char *
VSGetInstanceLocalizedName(NSRunningApplication *instance)
{
    if (!instance)
        return NULL;

    return NSStringToUTF8Dup(instance.localizedName);
}

VS_EXTERN char *
VSGetInstanceVersion(NSRunningApplication *instance)
{
    if (!instance || !instance.bundleURL)
        return NULL;

    NSBundle *bundle = [NSBundle bundleWithURL: instance.bundleURL];
    if (!bundle)
        return NULL;

    id versionValue = [bundle objectForInfoDictionaryKey: @"CFBundleVersion"];
    if (versionValue && [versionValue isKindOfClass: [NSString class]])
        return NSStringToUTF8Dup((NSString *)versionValue);

    return NULL;
}

int main(int argc, char **argv)
{
    NSRunningApplication **instances = VSQueryRunningInstances();

    for (int i = 0; instances && instances[i]; i++) {
        NSRunningApplication *instance = instances[i];

        pid_t pid = VSGetInstanceProcessIdentifier(instance);
        char *name = VSGetInstanceLocalizedName(instance);
        char *version = VSGetInstanceVersion(instance);
        char *bunPath = VSGetInstanceBundlePath(instance);
        char *exePath = VSGetInstanceExecutablePath(instance);
        char *slnPath = VSQueryInstanceCurrentSelectedSolutionPath(instance);

        printf("%s %s\n", name, version);
        printf("  pid: %u\n", pid);
        printf("  bun: %s\n", bunPath);
        printf("  exe: %s\n", exePath);
        printf("  sln: %s\n", slnPath);

        free(name);
        free(version);
        free(bunPath);
        free(exePath);
        free(slnPath);
    }

    VSFreeRunningInstances(instances);

    return 0;
}
