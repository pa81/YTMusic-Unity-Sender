// Listens for messages from content.js
browser.runtime.onMessage.addListener((songInfo, sender) => {
  console.log("Background script RECEIVED this object:", songInfo);

  const jsonBody = JSON.stringify(songInfo);

  console.log("Background script SENDING this JSON:", jsonBody);

  fetch('http://localhost:8080/', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: jsonBody,
  })
  .then(response => console.log("Unity responded with status:", response.status))
  .catch(error => console.error("Background script FETCH FAILED:", error));

  return true;
});