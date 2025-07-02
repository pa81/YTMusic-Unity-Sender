let currentSongInfo = {};

setInterval(() => {
    const playerBar = document.querySelector("ytmusic-player-bar");
    if (!playerBar) return;

    const titleEl = playerBar.querySelector(".title.ytmusic-player-bar");
    const artistEl = playerBar.querySelector(".byline.ytmusic-player-bar");
    const thumbnailEl = playerBar.querySelector("img");
    const timeInfoEl = playerBar.querySelector(".time-info.ytmusic-player-bar");

    if (!titleEl || !artistEl || !thumbnailEl || !timeInfoEl) return;
    
    const timeParts = timeInfoEl.innerText.split(' / ');
    if (timeParts.length !== 2) return;
    
    const newSongInfo = {
        title: titleEl.innerText,
        artist: artistEl.innerText,
        thumbnailUrl: thumbnailEl.src.replace(/=w60-h60/, '=w544-h544'),
        currentTime: timeParts[0].trim(),
        totalDuration: timeParts[1].trim()
    };

    if (newSongInfo.title && JSON.stringify(currentSongInfo) !== JSON.stringify(newSongInfo)) {
        currentSongInfo = newSongInfo;
        browser.runtime.sendMessage(currentSongInfo);
    }
}, 1500);