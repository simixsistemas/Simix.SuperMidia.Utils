// psneuter.c, written by scotty2.

// neuter the android property service.

// ashmem allows us to restrict permissions for a page further, but not relax them.
// adb relies on the ability to read ro.secure to know whether to drop its privileges or not;
// if it can't read the ro.secure property (because perhaps it couldn't map the ashmem page... :)
// then it will come up as root under the assumption that ro.secure is off.
// this will have the unfortunate side effect of rendering any of the bionic userspace that relies on the property
// service and things like dns broken.
// thus, we will want to use this, see if we can fix the misc partition, and downgrade the firmware as a whole to something more root friendly.

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <sys/mman.h>
#include <sys/ioctl.h>
#include <sys/types.h>
#include <linux/ioctl.h>
#include <signal.h>
#include <unistd.h>
#include <fcntl.h>
#include <dirent.h>
#include <stdint.h>

#define ASHMEM_NAME_LEN         256
#define __ASHMEMIOC             0x77
#define ASHMEM_SET_NAME         _IOW(__ASHMEMIOC, 1, char[ASHMEM_NAME_LEN])
#define ASHMEM_GET_NAME         _IOR(__ASHMEMIOC, 2, char[ASHMEM_NAME_LEN])
#define ASHMEM_SET_SIZE         _IOW(__ASHMEMIOC, 3, size_t)
#define ASHMEM_GET_SIZE         _IO(__ASHMEMIOC, 4)
#define ASHMEM_SET_PROT_MASK    _IOW(__ASHMEMIOC, 5, unsigned long)
#define ASHMEM_GET_PROT_MASK    _IO(__ASHMEMIOC, 6)
#define ASHMEM_PIN              _IOW(__ASHMEMIOC, 7, struct ashmem_pin)
#define ASHMEM_UNPIN            _IOW(__ASHMEMIOC, 8, struct ashmem_pin)
#define ASHMEM_GET_PIN_STATUS   _IO(__ASHMEMIOC, 9)
#define ASHMEM_PURGE_ALL_CACHES _IO(__ASHMEMIOC, 10)

int main(int argc, char **argv, char **envp)
{
    char *workspace;
    char *fdStr;
    char *szStr;

    char *ppage;

    int fd;
    long sz;

    DIR *dir;
    struct dirent *dent;
    char cmdlinefile[PATH_MAX];
    char cmdline[PATH_MAX];

    pid_t adbdpid = 0;

    setvbuf(stdout, 0, _IONBF, 0);
    setvbuf(stderr, 0, _IONBF, 0);

    workspace = getenv("ANDROID_PROPERTY_WORKSPACE");

    if(!workspace)
    {
	fprintf(stderr, "Couldn't get workspace.\n");
	exit(1);
    }

    fdStr = workspace;
    if(strstr(workspace, ","))
	*(strstr(workspace, ",")) = 0;
    else
    {
	fprintf(stderr, "Incorrect format of ANDROID_PROPERTY_WORKSPACE environment variable?\n");
	exit(1);
    }
    szStr = fdStr + strlen(fdStr) + 1;

    fd = atoi(fdStr);
    sz = atol(szStr);

    if((ppage = mmap(0, sz, PROT_READ, MAP_SHARED, fd, 0)) == MAP_FAILED)
    {
	fprintf(stderr, "mmap() failed. %s\n", strerror(errno));
	exit(1);
    }

    if(ioctl(fd, ASHMEM_SET_PROT_MASK, 0))
    {
	fprintf(stderr, "Failed to set prot mask (%s)\n", strerror(errno));
	exit(1);
    }

    printf("property service neutered.\n");
    printf("killing adbd. (should restart in a second or two)\n");

    // now kill adbd.

    dir = opendir("/proc");
    if(!dir)
    {
	fprintf(stderr, "Failed to open /proc? kill adbd manually... somehow\n");
	exit(1);
    }
    while((dent = readdir(dir)))
    {
	if(strspn(dent->d_name, "0123456789") == strlen(dent->d_name))
	{
	    // pid dir
	    strcpy(cmdlinefile, "/proc/");
	    strcat(cmdlinefile, dent->d_name);
	    strcat(cmdlinefile, "/cmdline");
	    if((fd = open(cmdlinefile, O_RDONLY)) < 0)
	    {
		fprintf(stderr, "Failed to open cmdline for pid %s\n", dent->d_name);
		continue;
	    }
	    if(read(fd, cmdline, PATH_MAX) < 0)
	    {
		fprintf(stderr, "Failed to read cmdline for pid %s\n", dent->d_name);
		close(fd);
		continue;
	    }
	    close(fd);
	    //	    printf("cmdline: %s\n", cmdline);
	    if(!strcmp(cmdline, "/sbin/adbd"))
	    {
		// we got it.
		adbdpid = atoi(dent->d_name);
		break;
	    }
	}
    }

    if(!adbdpid)
    {
	fprintf(stderr, "Failed to find adbd pid :(\n");
	exit(1);
    }

    if(kill(adbdpid, SIGTERM))
    {
	fprintf(stderr, "Failed to kill adbd (%s)\n", strerror(errno));
	exit(1);
    }
    return 0;
}
