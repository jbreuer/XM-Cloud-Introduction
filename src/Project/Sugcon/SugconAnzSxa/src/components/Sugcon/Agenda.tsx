import {
  withDatasourceCheck,
  GetStaticComponentProps,
  useComponentProps,
  Text,
  ComponentRendering,
  Field,
} from '@sitecore-jss/sitecore-jss-nextjs';
import { fetchSessionizeData } from 'lib/sessionize/fetch-sessonize-data';
import { ComponentData } from 'lib/sessionize/sessionizeData';
import { layoutServiceFactory } from 'lib/layout-service-factory';
import React, { useEffect, useState } from 'react';

interface Fields {
  SessionizeURL: Field<string>;
}

type AgendaRendering = ComponentRendering & {
  fields: Fields;
};

const Agenda = (): JSX.Element => (
  <div>
    <p>Agenda Component</p>
  </div>
);

export const getStaticProps: GetStaticComponentProps = async (rendering: AgendaRendering) => {
  const sessionizeAgendaUrl = rendering?.fields?.SessionizeURL.value;
  return await fetchSessionizeData(sessionizeAgendaUrl);
};

export const Default = (props: ComponentData): JSX.Element => {
  const externalData = useComponentProps<string>(props.rendering.uid);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      const layoutService = layoutServiceFactory.create("sugconanz");
      try {
        const data = await layoutService.fetchLayoutData("/");
        console.log('data', data);
        setLoading(false);
      } catch (error) {
        console.error('Failed to fetch data:', error);
        setLoading(false);
      }
    };

    fetchData();
  }, []);  // Empty dependency array means this effect will only run once after the initial render

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
      <div className="container component">
        <h1 className="p-3">
          <Text field={props?.fields?.Title} />
        </h1>
        <div dangerouslySetInnerHTML={{ __html: externalData }} />
      </div>
  );
};

export default withDatasourceCheck()(Agenda);
