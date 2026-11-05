const fs=require('node:fs');
const path=require('node:path');
const {chromium}=require('@playwright/test');
(async()=>{
 const root=path.resolve(__dirname,'..');
 const project=JSON.parse(fs.readFileSync(path.join(root,'project.json'),'utf8'));
 let composition='';
 for(const target of [...project.targets].sort((a,b)=>a.layerOrder-b.layerOrder)){
  if(target.visible===false)continue;
  const costume=target.costumes[target.currentCostume];
  const asset=fs.readFileSync(path.join(root,'assets',costume.md5ext),'utf8');
  composition+=`<g transform="translate(${240+(target.x||0)-costume.rotationCenterX},${180-(target.y||0)-costume.rotationCenterY})">${asset}</g>`;
 }
 for(const monitor of project.monitors){
  const value=project.targets[0].variables[monitor.id][1];
  composition+=`<g transform="translate(${monitor.x},${monitor.y})"><rect width="110" height="21" rx="3" fill="#25344f"/><text x="6" y="14" font-family="monospace" font-size="11" fill="#c2d6ed">${monitor.params.VARIABLE}</text><text x="102" y="14" text-anchor="end" font-family="monospace" font-size="12" font-weight="bold" fill="#f6d688">${value}</text></g>`;
 }
 const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="720" height="540" viewBox="0 0 480 360">${composition}</svg>`;
 fs.writeFileSync(path.join(root,'docs','preview.svg'),svg);
 const browser=await chromium.launch({headless:true});
 const page=await browser.newPage({viewport:{width:720,height:540},deviceScaleFactor:1});
 await page.setContent(`<html><body style="margin:0">${svg}</body></html>`);
 await page.screenshot({path:path.join(root,'docs','preview.png')});
 await browser.close();console.log('Saved original-costume layout preview.');
})().catch(e=>{console.error(e);process.exit(1)});
